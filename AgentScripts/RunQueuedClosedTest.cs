var callbacks = UnityEditor.EditorApplication.delayCall.GetInvocationList();
if (callbacks.Length != 1 || callbacks[0].Method.Module.Assembly.GetName().Name != "PipelineEval_bda9ddbd")
    throw new System.Exception("Unexpected callback; preserve the Editor queue.");
var own = (UnityEditor.EditorApplication.CallbackFunction)callbacks[0];
UnityEditor.EditorApplication.delayCall -= own;
own();
return new { state = System.IO.File.ReadAllText("Artifacts/ClosedTest/BuildState.json") };
