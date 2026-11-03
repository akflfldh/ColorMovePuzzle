using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.TestTools;

namespace ColorMovePuzzle.Tests
{
    public sealed class PuzzleAndroidStartupTests
    {
        [Test]
        public void EarlyEnhancedTouchEnableThenSubsystemResetReproducesNativeDeviceException()
        {
            // Mirrors the two package SubsystemRegistration callbacks, not game movement code.
            // Android AppUI enables EnhancedTouch; InputSystem's later callback resets its struct
            // but leaves the registered delegates alive. Desktop AppUI skips the mobile Enable.
            var touchType=typeof(UnityEngine.InputSystem.EnhancedTouch.Touch);
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            var global=touchType.GetField("s_GlobalState",flags);
            var initialize=touchType.GetMethod("InitializeGlobalTouchState",flags);
            Assert.That(global,Is.Not.Null);Assert.That(initialize,Is.Not.Null);
            if(EnhancedTouchSupport.enabled)Assert.Ignore("Run this isolated startup reproduction before another tool enables EnhancedTouch.");
            object original=global.GetValue(null),enabledState=null;
            Touchscreen device=null;
            try
            {
                EnhancedTouchSupport.Enable();enabledState=global.GetValue(null);
                Assert.That(EnhancedTouchSupport.enabled,Is.True);
                initialize.Invoke(null,null);
                Assert.That(EnhancedTouchSupport.enabled,Is.False,"The package reset loses the enable count and update mask.");
                LogAssert.Expect(LogType.Exception,new Regex("ArgumentException: 'InputUpdateType.None' is not a valid update mask"));
                LogAssert.Expect(LogType.Error,new Regex("ArgumentException while executing 'InputSystem.onDeviceChange' callbacks"));
                device=InputSystem.AddDevice<Touchscreen>("AndroidStartupReproduction");
                Assert.That(device.added,Is.True,"Raw Input System/UI can survive while EnhancedTouch's callback fails.");
            }
            finally
            {
                // Restore valid state before device removal; never leave the intentionally broken
                // third-party singleton or callbacks in place for the remaining test suite.
                var deviceCallback=(Action<InputDevice,InputDeviceChange>)Delegate.CreateDelegate(typeof(Action<InputDevice,InputDeviceChange>),
                    typeof(EnhancedTouchSupport).GetMethod("OnDeviceChange",flags));
                InputSystem.onDeviceChange-=deviceCallback;
                if(enabledState!=null)global.SetValue(null,enabledState);
                if(device!=null&&device.added)InputSystem.RemoveDevice(device);
                EnhancedTouchSupport.Disable();global.SetValue(null,original);
            }
        }

        [Test]
        public void AndroidPlayerExcludesEditorOnlyAppUiWithoutChangingInputPackage()
        {
            var settings=Unity.AppUI.Core.AppUI.settings;
            Assert.That(settings.editorOnly,Is.True);
            var symbols=PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Android).Split(';');
            Assert.That(symbols,Does.Contain("APP_UI_EDITOR_ONLY"));
            var assemblies=CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a=>a.name).ToArray();
            Assert.That(assemblies,Does.Not.Contain("Unity.AppUI"));
            Assert.That(assemblies,Does.Contain("Unity.InputSystem"));
            Assert.That(assemblies,Does.Contain("ColorMovePuzzle.Runtime"));
            Assert.That(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(InputSystem).Assembly).version,Is.EqualTo("1.20.0"));
            Assert.That(InputSystem.settings.updateMode,Is.EqualTo(InputSettings.UpdateMode.ProcessEventsInDynamicUpdate));
        }
    }
}
