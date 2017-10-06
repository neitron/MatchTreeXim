
using UnityEngine;
using DG.Tweening;



[RequireComponent(typeof(SpriteRenderer))]
public class Candy : MonoBehaviour
{


	Vector2 cellPosition;
	SpriteRenderer renderer;



	public static Candy create(Candy originalCandy, Vector2 cellPos, Vector2 spawnPos, Sprite candy, Transform parent)
	{
		var temp = Instantiate(originalCandy, spawnPos, Quaternion.identity, parent);
		temp.renderer = temp.GetComponent<SpriteRenderer>();
		temp.renderer.sprite = candy;
		temp.cellPosition = cellPos;

		temp.transform.DOMove(cellPos, 2.0f).SetEase(Ease.OutBounce);

		return temp;
	}


	private void OnMouseDown()
	{
		transform.localScale *= 1.5f;
		renderer.sortingOrder += 1;
		transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
	}


	private void OnMouseDrag()
	{
		if (Vector2.Distance(cellPosition, transform.position) < 1.0f)
			transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
		else
			OnMouseUp();
	}


	private void OnMouseUp()
	{
		renderer.sortingOrder -= 1;
		transform.localScale = Vector3.one;
		transform.position = cellPosition;
		transform.DOShakePosition(0.5f, 0.1f).OnComplete(delegate { transform.position = cellPosition; });
	}


}
