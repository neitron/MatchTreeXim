
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class CandyCell : MonoBehaviour
{


	new SpriteRenderer renderer;

	Vector2 anchor;
	float scale;

	Vector2 cell;
	public Candy candy { get; protected set; }
	public bool isFree { get; protected set; }

	Board board { get { return Board.instance; } }

	bool isFocused;

	public Text debText;
	public void updateDebInfo(Camera c)
	{
		debText.text = cell.ToString("F0") + "\n" + candy.type;
		debText.transform.position = c.WorldToScreenPoint(transform.position);
	}


	public static CandyCell create(CandyCell original, Vector2 cell, Vector2 anchor, Vector2 spawnPos, Candy candy, Transform parent, float scale)
	{
		CandyCell temp = Instantiate(original, spawnPos, Quaternion.identity, parent);
		temp.renderer = temp.GetComponent<SpriteRenderer>();
		temp.renderer.sprite = candy.mainSprite;
		temp.anchor = anchor;
		temp.scale = scale;
		temp.transform.localScale = new Vector3(scale, scale, scale);
		temp.transform.DOMove(anchor, 2.0f).SetEase(Ease.OutBounce);
		temp.candy = candy;
		temp.cell = cell;

		return temp;
	}
	

	public static void swap(ref CandyCell c0, ref CandyCell c1, Action callback)
	{
		CandyCell cTemp = c0;
		c0 = c1;
		c1 = cTemp;

		Vector2 cellTemp = c0.cell;
		c0.cell = c1.cell;
		c1.cell = cellTemp;

		if(c0.isFree && c1.isFree)
		{
			Vector3 posTemp = c0.transform.position;
			c0.transform.position = c1.transform.position;
			c1.transform.position = cellTemp;

			if (callback != null)
				callback();
		}
		else
		{
			c0.transform.DOMove(c1.transform.position, 0.2f).SetEase(Ease.OutBounce);
			c1.transform.DOMove(c0.transform.position, 0.2f).SetEase(Ease.OutBounce).OnComplete(delegate
			{ if (callback != null) callback(); });
		}
	}


	public static void failSwap(CandyCell c0, CandyCell c1)
	{
		Tweener t0 = null;
		t0 = c0.transform.DOMove(c1.transform.position, 0.2f)
			//.SetEase(Ease.OutBounce)
			.SetAutoKill(false)
			.OnComplete(delegate { t0.PlayBackwards(); });

		Tweener t1 = null;
		t1 = c1.transform.DOMove(c0.transform.position, 0.2f)
			//.SetEase(Ease.OutBounce)
			.SetAutoKill(false)
			.OnComplete(delegate { t1.PlayBackwards(); });
	}


	public static void fallingSwap(ref CandyCell c0, ref CandyCell c1, Action callback)
	{
		CandyCell cTemp = c0;
		c0 = c1;
		c1 = cTemp;

		Vector2 cellTemp = c0.cell;
		c0.cell = c1.cell;
		c1.cell = cellTemp;

		if (c0.isFree && c1.isFree)
		{
			Vector3 posTemp = c0.transform.position;
			c0.transform.position = c1.transform.position;
			c1.transform.position = cellTemp;

			if (callback != null)
				callback();
		}
		else
		{
			c0.transform.DOMove(c1.transform.position, 0.1f).SetEase(Ease.Linear);
			c1.transform.DOMove(c0.transform.position, 0.1f).SetEase(Ease.Linear).OnComplete(delegate
			{ if (callback != null) callback(); });
		}
	}


	private void OnMouseOver()
	{
		if(isFocused)
		{
			return;
		}

		Board.instance.OnCandySelected(cell);
	}


	private void OnMouseDown()
	{
		isFocused = true;

		transform.DOScale(scale * 1.5f, 0.1f).SetEase(Ease.OutBack);
		renderer.sortingOrder += 5;

		Board.instance.OnCandyFocused(cell);
	}


	private void OnMouseUp()
	{
		isFocused = false;

		renderer.sortingOrder -= 5;
		transform.DOScale(scale, 0.1f);

		Board.instance.OnCandyUnfocused(cell);
	}


	/// <summary>
	/// Returns true if candies of the same type
	/// </summary>
	public static bool operator ==(CandyCell c0, CandyCell c1)
	{
		if (c0 && c1)
		{
			return c0.candy.type == c1.candy.type;
		}
		else if (!c0 && !c1)
		{
			return true;
		}
		return false;
	}


	/// <summary>
	/// Returns true if candies have different types
	/// </summary>
	public static bool operator !=(CandyCell c0, CandyCell c1)
	{
		if (c0 == c1)
		{
			return false;
		}
		return true;
	}


	public static bool isAvalible(CandyCell c0)
	{
		if (c0 != null && c0.isFree)
		{
			return true;
		}
		return false;
	}


	internal void Despawn(List<Vector2> pool)
	{
		isFree = true;
		pool.Add(cell);
		transform.DOScale(0.2f, 0.5f).SetEase(Ease.OutBounce).OnComplete(delegate { /*gameObject.SetActive(false);*/  });
	}


}
