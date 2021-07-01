#region using
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#endregion

namespace C2M2.NeuronalDynamics.UGX
{
    /// Attachment
    /// <summary>
    /// Marker interface for attachments
    /// </summary>
    /// All (new) attachments must implement the Attachment interface
    public interface Attachment { }

    /// IAttachmentData
    /// <summary>
    /// Marker interface for attachment data
    /// </summary>
    /// All (new) data attachments must implement the IAttachmentData interface
    public interface IAttachmentData { }

    /// IAttachment
    /// <summary>
    /// Abstract base class for all attachments providing common functionality
    /// </summary>
    /// An attachment stores internally a list of type T which encapsulates
    /// the actual attachment data. The type needs to be derived from the 
    /// common IAttachmentData marker interface for attachment data
    /// <typeparam name="T"> Attachment data's type </typeparam>
    public class IAttachment<T> : Attachment, ICloneable where T : IAttachmentData
    {
        /// <summary>
        /// Encapsulate the attachment data in a list of type T
        /// <returns> Attachment's data as a list of type IAttachmentData </returns>
        /// </summary>
        public List<T> Data
        {
            get; /// Returns the stored data in the attachment
            set; /// Saves the stored data in the attachment
        }

        /// Clone
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            IAttachment<T> clone = new IAttachment<T>();
            clone.Data = Data.GetRange(0, Data.Count);
            return clone;
        }
    }
    
    /// AttachmentHandler
    /// <summary>
    /// The attachment handler allows managing attachments of grids
    /// </summary>
    /// Note: AttachmentHandler could be a singleton
    public static class AttachmentHandler
    {
        // Registered attachments we are allowed to use
        public static Dictionary<string, Type> attachments = new Dictionary<string, Type>();

        // All registered attachments will be considered for parallel distribution
        /// <summary>
        /// Distribute mesh and attachments for grid in parallel
        /// </summary>
        /// <param name="grid"> a grid </param>
        public static void Distribute(in Grid grid)
        {
            UnityEngine.Debug.LogError("Currently not implemented");
        }

        /// Attach
        /// <summary>
        /// Attaches an attachment to the grid if attachment has been
        /// registered and wasn't attached already to another grid
        /// </summary>
        ///  Type ot attachment will be inferred by compiler
        ///  If no value specified for clone parameter, attachment data will be cloned before inserting
        /// <typeparam name="T"> Attachment's type </typeparam>
        /// <param name="grid"> Grid where data should be attached to </param>
        /// <param name="iattachment"> Actual attachment </param>
        /// <param name="clone"> If true attachment data will be cloned </param>
        public static void Attach<T>(this Grid grid, IAttachment<T> iattachment, in Boolean clone=true) 
            where T : IAttachmentData
        {
            // Name of requested attachment type 
            string name = attachments.FirstOrDefault(x => x.Value == typeof(IAttachment<T>)).Key;

            // Check if attachment was registered
            if (name == null)
            {
                Debug.LogError($"Trying to attach an unregistered attachment " +
                    $"with type >>{typeof(T)}<< which ist not meaningful");
                return;
            }

            // If attachment already present on grid, don't re-attach as it will destroy data
            if (grid.HasVertexAttachment<T>())
            {
                Debug.LogWarning($"Trying to re-attach attachment with name >>{name}<< which is not meaningful");
                return;
            }

            /// Otherwise initialize the registered attachment
            int size = grid.Mesh.vertices.Length;
            iattachment.Data = new List<T>(new T[size]);
            grid.AttachmentInfo.Add(name, typeof(IAttachment<T>),
                (IAttachment<T>) (clone ? iattachment.Clone() : iattachment));
        }

        /// GetName
        /// <summary>
        /// Get the name of the attachment by type
        /// </summary>
        /// <typeparam name="T"> Type of attachment </typeparam>
        /// <returns></returns>
        public static string GetName<T>(this Grid grid)
        {
            return grid.AttachmentInfo.Types.FirstOrDefault(x => x.Value == typeof(T)).Key;
        }

        /// Detach
        /// <summary>
        /// Detach the attachment
        /// Type ot attachment will be inferred by compiler
        /// </summary>
        /// <typeparam name="T"> Type of attachment </typeparam>
        /// <param name="name"> Actual attachment's name</param>
        public static void Detach<T>(this Grid grid, Attachment attachment) where T : IAttachmentData
        {
            if (!grid.AttachmentInfo.Data.ContainsValue(attachment))
            {
                string name = grid.GetName<T>();
                Debug.LogError($"Trying to detach an (unattached yet registered)" +
                    $" attachment with name >>{name}<< which is not meanigful");
                return;
            }
            grid.Clear(grid.GetName<T>());
        }

        /// Register
        /// <summary>
        /// Register an attachment
        /// </summary>
        /// <typeparam name="T"> Attachment type </param>
        /// <param name="name"> Name of attachment </param>
        private static void Register<T>(in string name)
        {
            if (attachments.ContainsKey(name))
            {
                Debug.LogError($">>{name}<< attachment is not meaningful");
            }

            attachments[name] = typeof(T);
        }

        /// Available
        /// <summary>
        /// List all available attachments by name
        /// </summary>
        public static string Available()
        {
            string s = "Available attachments (Name/Type): ";
            foreach (var pair in attachments)
            {
                s += $"\n\t{pair.Key} => {pair.Value}";
            }
            return s;
        }

        /// Attachment Registry
        static AttachmentHandler()
        {
            Register<IAttachment<DiameterData>>("diam");
            Register<IAttachment<MappingData>>("npMapping");
            Register<IAttachment<SynapseData>>("synapses");
            Register<IAttachment<NormalData>>("npNormals");
            Register<IAttachment<IndexData>>("pid");
        }
    }
}
