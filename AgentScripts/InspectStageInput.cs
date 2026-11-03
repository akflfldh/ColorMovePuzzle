var flow = UnityEngine.Object.FindAnyObjectByType<ColorMovePuzzle.PuzzleCampaignController>();
var card = flow.Selection.Buttons[0];
var rect = card.GetComponent<UnityEngine.RectTransform>();
var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {
    position = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rect.position)
};
var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
var callback = card.GetType().GetField("selected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(card);
return Newtonsoft.Json.JsonConvert.SerializeObject(new {
    card = card.name, index = card.StageNumber, locked = card.Locked, callback = callback != null,
    active = card.gameObject.activeInHierarchy, interactable = card.Button.IsInteractable(),
    x = pointer.position.x, y = pointer.position.y,
    hits = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(hits, h => h.gameObject.name)),
    clickEnabled = UnityEngine.Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().leftClick.action.enabled
}, Newtonsoft.Json.Formatting.Indented);
