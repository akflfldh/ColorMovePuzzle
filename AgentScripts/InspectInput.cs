var module = UnityEngine.Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
var system = UnityEngine.EventSystems.EventSystem.current;
return Newtonsoft.Json.JsonConvert.SerializeObject(new {
    focused = UnityEngine.Application.isFocused,
    play = UnityEngine.Application.isPlaying,
    eventSystemEnabled = system.enabled,
    inputModuleEnabled = module.enabled,
    moduleActive = module.IsActive(),
    actions = module.actionsAsset == null ? "null" : module.actionsAsset.name,
    click = module.leftClick == null || module.leftClick.action == null ? "null" : module.leftClick.action.ToString(),
    clickEnabled = module.leftClick != null && module.leftClick.action != null && module.leftClick.action.enabled,
    point = module.point == null || module.point.action == null ? "null" : module.point.action.ToString(),
    pointEnabled = module.point != null && module.point.action != null && module.point.action.enabled,
    updateMode = UnityEngine.InputSystem.InputSystem.settings.updateMode.ToString(),
    backgroundBehavior = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior.ToString(),
    editorInputBehavior = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode.ToString()
}, Newtonsoft.Json.Formatting.Indented);
