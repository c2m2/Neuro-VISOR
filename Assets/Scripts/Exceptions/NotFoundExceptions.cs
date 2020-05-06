using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace C2M2
{
    public class MeshNotFoundException : Exception
    {
        public MeshNotFoundException() { }
        public MeshNotFoundException(string message) : base(message) { }
        public MeshNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class MeshFilterNotFoundException : Exception
    {
        public MeshFilterNotFoundException() { }
        public MeshFilterNotFoundException(string message) : base(message) { }
        public MeshFilterNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class RigidbodyNotFoundException : Exception
    {
        public RigidbodyNotFoundException() { }
        public RigidbodyNotFoundException(string message) : base(message) { }
        public RigidbodyNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class OVRGrabbableNotFoundException : Exception
    {
        public OVRGrabbableNotFoundException() { }
        public OVRGrabbableNotFoundException(string message) : base(message) { }
        public OVRGrabbableNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class SimulationNotFoundException : Exception
    {
        public SimulationNotFoundException() : base() { }
        public SimulationNotFoundException(string message) : base(message) { }
        public SimulationNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class RaycastValueControllerNotFoundException : Exception
    {
        public RaycastValueControllerNotFoundException() : base() { }
        public RaycastValueControllerNotFoundException(string message) : base(message) { }
        public RaycastValueControllerNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
    public class ValuesNotFoundException : Exception
    {
        public ValuesNotFoundException() { }
        public ValuesNotFoundException(string message) : base(message) { }
        public ValuesNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
