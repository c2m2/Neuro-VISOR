using System;

namespace C2M2.NeuronalDynamics.UGX
{
    /// SynapseType
    /// <summary>
    /// Represent the type of synapse
    /// </summary>
    public enum SynapseType : byte
    {
        UNDEF, // no synapse type associated with this vertex
        ALPHA_POST, // alpha post synapse
        EXP2 // bi-exponential (pre) synapse
    }

    /// <summary>
    /// A synapse respresentation
    /// </summary>
    public interface ISynapse
    {
        /// GetType
        /// <summary>
        /// Type of synapse
        /// </summary>
        /// <returns> Type of synapse</returns>
        SynapseType GetType();

        /// <summary>
        /// Get the current of synapse at time t
        /// </summary>
        /// <param name="t"> Time </param>
        /// <returns> Current of synapse [A] </returns>
        float GetCurrent(in float t);
    }

    /// Exp2Synapse
    /// <summary>
    /// A bi-exponential synapse
    /// </summary>
    public readonly struct EXP2Synapse : ISynapse
    {
        /// <summary>
        /// Onset [s]
        /// </summary>
        readonly float m_onset;
        /// <summary>
        ///  Condutance [S]
        /// </summary>
        readonly float m_gMax;
        /// <summary>
        /// First time constant [s]
        /// </summary>
        readonly float m_tau1;
        /// <summary>
        /// Second time constant [s]
        /// </summary>
        readonly float m_tau2;
        /// <summary>
        /// Reversal potential [V]
        /// </summary>
        readonly float m_rev;

        /// <see cref="ISynapse.GetCurrent"/>
        float ISynapse.GetCurrent(in float time)
        {
            return 0;
        }

        /// <see cref="ISynapse.GetType"/>
        SynapseType ISynapse.GetType()
        {
            return SynapseType.EXP2;
        }

        /// <summary>
        /// Construct an empty EXP2 synapse
        /// </summary>
        /// <param name="rev"></param>
        /// <param name="gmax"></param>
        /// <param name="onset"></param>
        /// <param name="tau1"></param>
        /// <param name="tau2"></param>
        public EXP2Synapse(float rev = 0, float gmax = 0, float onset = 0, float tau1 = 0, float tau2 = 0)
        {
            m_rev = rev;
            m_gMax = gmax;
            m_onset = onset;
            m_tau1 = tau1;
            m_tau2 = tau2;
        }
    }

    /// AlphaPostSynapse
    /// <summary>
    /// A alpha post synapse
    /// </summary>
    public readonly struct AlphaPostSynapse : ISynapse
    {
        /// <summary>
        /// Onset [s]
        /// </summary>
        readonly float m_onset;
        /// <summary>
        /// Conductivity [S]
        /// </summary>
        readonly float m_gMax;
        /// <summary>
        /// Time constant [s]
        /// </summary>
        readonly float m_tau;
        /// <summary>
        /// Reversal potential [mV]
        /// </summary>
        readonly float m_rev;

        /// <see cref="ISynapse.GetCurrent"/>
        float ISynapse.GetCurrent(in float time)
        {
            return 0;
        }

        /// <see cref="ISynapse.GetType"/>
        SynapseType ISynapse.GetType()
        {
            return SynapseType.ALPHA_POST;
        }

        /// <summary>
        /// Construct an empty alpha post synapse
        /// </summary>
        /// <param name="onset"></param>
        /// <param name="gmax"></param>
        /// <param name="tau"></param>
        /// <param name="rev"></param>
        public AlphaPostSynapse(float onset = 0, float gmax = 0, float tau = 0, float rev = 0)
        {
            m_onset = onset;
            m_gMax = gmax;
            m_tau = tau;
            m_rev = rev;
        }
    }

    /// <summary>
    /// Undefined Synapse
    /// </summary>
    public readonly struct UndefSynapse : ISynapse
    {
        /// <see cref="ISynapse.GetCurrent"/>
        float ISynapse.GetCurrent(in float time)
        {
            throw new NotImplementedException();
        }

        /// <see cref="ISynapse.GetType"/>
        SynapseType ISynapse.GetType()
        {
            return SynapseType.UNDEF;
        }
    }
}
