using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// Realy dirty hot prototype
/// </summary>
public class MapSelector : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
	public static int currentMap = 0;

	Vector2 anchor;

	RectTransform rt;

	void Awake()
	{
		rt = GetComponent<RectTransform>();
	}

	void Start()
	{
		anchor = rt.anchoredPosition;
	}

	void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
	{
		anchor = rt.anchoredPosition;
	}

	void IDragHandler.OnDrag(PointerEventData eventData)
	{
		var vs = eventData.delta;
		vs.y = 0;
		rt.anchoredPosition += vs;
	}

	void IEndDragHandler.OnEndDrag(PointerEventData eventData)
	{
		Vector2 dir = rt.anchoredPosition - anchor;
		if (dir.magnitude > Screen.width * 0.4 && currentMap - (int)Mathf.Sign(dir.x) < 6 && currentMap - (int)Mathf.Sign(dir.x) >= 0)
		{
			currentMap -= (int)Mathf.Sign(dir.x);

			rt.DOAnchorPos(anchor + Vector2.right * Mathf.Sign(dir.x) * Screen.width, 0.2f)
				.SetEase(Ease.InBack)
				.OnComplete(delegate
				{ anchor = rt.anchoredPosition; });
		}
		else
		{
			rt.DOAnchorPos(anchor, 0.2f).SetEase(Ease.OutBack);
		}
	}
}
