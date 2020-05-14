#region using
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    /// Note/TODO: Could enhance this as a generic attachment accessor also for Edges
    /// Basically use this as AttachmentAccessor<T, E> with T being the
    /// attachment data and E the element type
    /// <summary>
    /// VertexAttachmentAcccesor
    /// </summary>
    public class VertexAttachementAccessor<T> : IEnumerable<T> where T : IAttachmentData
    {
        /// associated grid
        private readonly Grid grid;

        /// VertexAttachmentAcccesor
        /// <summary>
        /// Create the accessor
        /// </summary>
        /// <param name="name"></param>
        /// 
        public VertexAttachementAccessor(in Grid grid)
        {
            this.grid = grid;
        }

        /// VertexAttachmentAcccesor
        /// <summary>
        /// Create the accesor with default data and a given size
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="size"></param>
        /// <param name="def"></param>
        public VertexAttachementAccessor(in Grid grid, int size, T def)
        {
            this.grid = grid;
            ((IAttachment<T>)grid.AttachmentInfo.Data[grid.GetName<IAttachment<T>>()]).Data = new List<T>();
            for (int i = 0; i < size; i++)
            {
                IAttachment<T> attachment = (IAttachment<T>)grid.AttachmentInfo.Data[grid.GetName<IAttachment<T>>()];
                attachment.Data.Add(def);
            }
        }

        /// GetValue
        /// <summary>
        /// Get's the value of the attachment
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private T GetValue(int index)
        {
            if (grid.HasVertexAttachment<T>())
            {
                Debug.LogError($"Grid >>{grid.Mesh.name}<< does not contain attachment type >>{typeof(T)}<<");
            }

            IAttachment<T> attachment = (IAttachment<T>)grid.AttachmentInfo.Data[grid.GetName<IAttachment<T>>()];

            var data = attachment.Data.ElementAtOrDefault(index);

            if (data == null)
            {
                Debug.LogError($"Trying to access attachment data of vertex with index {index} but no attachment data " +
                                     $"of type >>{typeof(T)}<< present for the required vertex index. Check input file for attachments!");
            }
            return data;
        }

        /// SetValue
        /// <summary>
        /// Set the value of the attachment
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        private void SetValue(int key, in T value)
        {
            if (grid.HasVertexAttachment<T>())
            {
                Debug.LogError($"Grid >>{grid.Mesh.name}<< does not contain attachment type >>{typeof(IAttachment<T>)}<<");
                return;
            }

            IAttachment<T> attachment = (IAttachment<T>)grid.AttachmentInfo.Data[grid.GetName<IAttachment<T>>()];

            attachment.Data[key] = value;
        }

        /// <summary>
        /// Get data
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T this[int key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        /// GetEnumerator
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            List<T> data = ((IAttachment<T>)grid.AttachmentInfo.Data[grid.GetName<IAttachment<T>>()]).Data;
            foreach (T t in data)
            {
                yield return t;
            }
        }

        /// GetEnumerator
        /// <summary>
        /// Enumerator implementation
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}