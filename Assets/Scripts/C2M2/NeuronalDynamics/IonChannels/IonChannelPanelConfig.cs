using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using C2M2.Interaction;
using C2M2.NeuronalDynamics.Simulation;

namespace C2M2.NeuronalDynamics.IonChannels
{
    public class IonChannelPanelConfig : MonoBehaviour
    {
        public Transform channelListParent;

        private static readonly Color ActiveColor   = new Color(0.18f, 0.80f, 0.18f);
        private static readonly Color InactiveColor = new Color(0.85f, 0.15f, 0.15f);
        private static Sprite circleSprite;

        private const float ButtonSize = 18f;
        private const float RowHeight  = 38f;
        private const float RowSpacing = 6f;

        private SparseSolverTestv1 solver;
        private readonly List<(IonChannel channel, Image buttonImage)> rows = new List<(IonChannel, Image)>();

        private void OnEnable()
        {
            if (GameManager.instance?.activeSims == null || GameManager.instance.activeSims.Count == 0)
            {
                Debug.LogWarning("IonChannelConfig: no SparseSolverTestv1 found in activeSims.");
                return;
            }

            solver = GameManager.instance.activeSims[0] as SparseSolverTestv1;

            if (solver == null || solver.ionChannels == null || solver.ionChannels.Count == 0)
            {
                Debug.LogWarning("IonChannelConfig: ionChannels not yet initialized.");
                return;
            }

            channelListParent = null;
            EnsureListParent();
            PopulateChannels();
        }

        private void EnsureListParent()
        {
            if (channelListParent != null) return;

            Transform bg = transform.Find("Background");
            if (bg == null)
            {
                Debug.LogError("IonChannelConfig: channelListParent not assigned and no 'Background' child found.");
                return;
            }

            Transform existing = bg.Find("ChannelList");
            if (existing != null)
            {
                channelListParent = existing;
                return;
            }

            GameObject listGO = new GameObject("ChannelList");
            listGO.transform.SetParent(bg, false);

            RectTransform listRect = listGO.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 1f);
            listRect.anchorMax = new Vector2(0.5f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = new Vector2(-5f, -45f);
            listRect.sizeDelta = new Vector2(270f, 280f);

            VerticalLayoutGroup vlg = listGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = RowSpacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 4, 4);

            channelListParent = listGO.transform;
        }

        private void Update()
        {
            if (solver == null)
            {
                if (GameManager.instance?.activeSims == null || GameManager.instance.activeSims.Count == 0) return;
                solver = GameManager.instance.activeSims[0] as SparseSolverTestv1;
                if (solver == null || solver.ionChannels == null || solver.ionChannels.Count == 0) return;
                channelListParent = null;
                EnsureListParent();
                PopulateChannels();
                return;
            }
            foreach (var (channel, btnImage) in rows)
                btnImage.color = solver.activeIonChannels.Contains(channel) ? ActiveColor : InactiveColor;
        }

        private void PopulateChannels()
        {
            foreach (Transform child in channelListParent)
                Destroy(child.gameObject);
            rows.Clear();

            if (circleSprite == null) circleSprite = MakeCircleSprite(64);

            foreach (IonChannel channel in solver.ionChannels)
                CreateRow(channel);
        }

        private void CreateRow(IonChannel channel)
        {
            GameObject rowGO = new GameObject(channel.Name + "_Row");
            rowGO.transform.SetParent(channelListParent, false);
            rowGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, RowHeight);

            HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(2, 2, 2, 2);

            GameObject btnGO = new GameObject("ToggleBtn");
            btnGO.transform.SetParent(rowGO.transform, false);
            btnGO.AddComponent<RectTransform>().sizeDelta = new Vector2(ButtonSize, ButtonSize);

            LayoutElement btnLayout = btnGO.AddComponent<LayoutElement>();
            btnLayout.minWidth = ButtonSize;
            btnLayout.minHeight = ButtonSize;
            btnLayout.preferredWidth  = ButtonSize;
            btnLayout.preferredHeight = ButtonSize;

            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.sprite = circleSprite;
            btnImage.color  = solver.activeIonChannels.Contains(channel) ? ActiveColor : InactiveColor;

            btnGO.layer = LayerMask.NameToLayer("Raycast");

            BoxCollider col = btnGO.AddComponent<BoxCollider>();
            col.size = new Vector3(ButtonSize, ButtonSize, 1f);

            RaycastPressEvents pressEvents = btnGO.AddComponent<RaycastPressEvents>();
            btnGO.AddComponent<RaycastEventManager>().LRTrigger = pressEvents;
            pressEvents.OnPress.AddListener(_ => OnToggleClicked(channel, btnImage));

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);

            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = channel.Name;
            label.fontSize  = 20f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            rows.Add((channel, btnImage));
        }

        private static Sprite MakeCircleSprite(int diameter)
        {
            Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[diameter * diameter];
            float r = diameter / 2f;
            Vector2 center = new Vector2(r - 0.5f, r - 0.5f);
            for (int i = 0; i < pixels.Length; i++)
            {
                float x = i % diameter;
                float y = i / diameter;
                pixels[i] = Vector2.Distance(new Vector2(x, y), center) <= r ? Color.white : Color.clear;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f));
        }

        private void OnToggleClicked(IonChannel channel, Image buttonImage)
        {
            if (solver == null) return;
            bool newState = !solver.activeIonChannels.Contains(channel);
            solver.ToggleChannel(channel, newState);
            if (newState) SparseSolverTestv1.SavedActiveChannelNames.Add(channel.Name);
            else SparseSolverTestv1.SavedActiveChannelNames.Remove(channel.Name);
            buttonImage.color = newState ? ActiveColor : InactiveColor;
        }
    }
}
