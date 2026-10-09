// Minimal stand-ins for com.unity.test-framework attributes so the PlayMode
// tests can be compile-checked outside Unity. Not used by the real project.
using System;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnityTestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnitySetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnityTearDownAttribute : Attribute { }
}
