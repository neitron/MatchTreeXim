
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;



[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class CandyCell : MonoBehaviour
{


	public Candy candy { get; protected set; }
	public bool isFree { get; protected set; }


	Board board { get { return Board.instance; } }
	

	new SpriteRenderer renderer;

	Vector2 anchor;
	Vector2 cell;

	float scale;
	bool isFocused;

	public static List<CandyCell> pool = new List<CandyCell>();



	public CandyCell init(Vector2 cell, Vector2 anchor, Vector2 spawnPos, Candy candy, Transform parent, float scale)
	{
		fallsAmount ++;
		transform.position = spawnPos;
		transform.parent = parent;
		renderer = GetComponent<SpriteRenderer>();
		renderer.sprite = candy.mainSprite;
		this.anchor = anchor;
		this.scale = scale;
		transform.localScale = Vector3.one * scale;
		transform.DOMove(anchor, 1.0f)
			.SetEase(Ease.OutBounce)
			.OnComplete(delegate
			{
				fallsAmount--;
			});
		this.candy = candy;
		this.cell = cell;

		return this;
	}
	

	public static void swap(ref CandyCell c0, ref CandyCell c1, Action callback)
	{
		if (swapBase(ref c0, ref c1))
		{
			c0.transform.DOMove(c0.anchor, c0.board.swapDuration).SetEase(Ease.OutBounce);
			c1.transform.DOMove(c1.anchor, c1.board.swapDuration).SetEase(Ease.OutBounce).OnComplete(delegate
			{ if (callback != null) callback(); });
		}
		else
		{
			if (callback != null)
			{
				callback();
			}
		}
	}


	public static void failSwap(CandyCell c0, CandyCell c1)
	{
		Tweener t0 = null;
		t0 = c0.transform.DOMove(c1.transform.position, 0.2f)
			.SetEase(Ease.OutExpo)
			.SetAutoKill(false)
			.OnComplete(delegate { t0.SetEase(Ease.InExpo).PlayBackwards(); });

		Tweener t1 = null;
		t1 = c1.transform.DOMove(c0.transform.position, 0.2f)
			.SetEase(Ease.OutExpo)
			.SetAutoKill(false)
			.OnComplete(delegate { t1.SetEase(Ease.InExpo).PlayBackwards(); });
	}


	public static void fallingSwap(ref CandyCell c0, ref CandyCell c1, Action callback)
	{
		if (swapBase(ref c0, ref c1))
		{
			fallingAnimation(c0, c1);
		}

		if (callback != null)
		{
			callback();
		}
	}


	public static int fallsAmount = 0;
	private static void fallingAnimation(CandyCell c0, CandyCell c1)
	{
		fallsAmount += 2;
		c0.transform.DOMove(c0.anchor, c0.board.fallingDuration).SetEase(c0.board.fallingEase).OnComplete(delegate
		{
			fallsAmount--;
		});
		c1.transform.DOMove(c1.anchor, c1.board.fallingDuration).SetEase(c1.board.fallingEase).OnComplete(delegate
		{
			fallsAmount--;
		});
	}


	private static bool swapBase(ref CandyCell c0, ref CandyCell c1)
	{
		CandyCell cTemp = c0;
		c0 = c1;
		c1 = cTemp;

		Vector2 cellTemp = c0.cell;
		c0.cell = c1.cell;
		c1.cell = cellTemp;

		Vector2 vector2Temp = c0.anchor;
		c0.anchor = c1.anchor;
		c1.anchor = vector2Temp;

		if (c0.isFree && c1.isFree)
		{
			c0.transform.position = c0.anchor;
			c1.transform.position = c1.anchor;

			return false;
		}

		return true;
	}


	private void OnMouseOver()
	{
		if(isFocused)
		{
			return;
		}

		board.OnCandySelected(cell);
	}


	private void OnMouseDown()
	{
		if(fallsAmount == 0)
		{
			isFocused = true;

			transform.DOScale(scale * 1.5f, 0.1f).SetEase(Ease.OutBack);
			renderer.sortingOrder += 5;

			board.OnCandyFocused(cell);

			board.ps.transform.position = anchor;
			board.ps.Play();
		}
	}


	private void OnMouseUp()
	{
		if (fallsAmount == 0)
		{
			isFocused = false;

			renderer.sortingOrder -= 5;
			transform.DOScale(scale, 0.1f);

			board.OnCandyUnfocused(cell);
		}
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


	public static bool operator ==(CandyCell c0, int c1)
	{
		if (c0)
		{
			return c0.candy.type == c1;
		}
		return false;
	}


	public static bool operator !=(CandyCell c0, int c1)
	{
		if (!c0)
		{
			return c0.candy.type != c1;
		}
		return false;
	}


	public static bool isReleased(CandyCell c0)
	{
		if (c0 != null && c0.isFree)
		{
			return true;
		}
		return false;
	}


	public static bool isNotReleased(CandyCell c0)
	{
		if (c0 != null && !c0.isFree)
		{
			return true;
		}
		return false;
	}


	public static void release(List<CandyCell> toRelease, Action callback)
	{
		int points = 10;

		if(toRelease.Count > 3)
		{
			Debug.Log("++++");
			var cc = toRelease[UnityEngine.Random.Range(0, toRelease.Count - 1)];
			toRelease.Remove(cc);
			cc.renderer.sprite = cc.candy.hSprite;
			points += 50;
		}

		int i = 0;
		for (; i < toRelease.Count - 1; i++)
		{
			points += 10;
			toRelease[i].release();
		}
		toRelease[i].release(callback);

		Board.instance.takePoints((toRelease.Count - 2) * points);
	}


	private void release(Action callback = null)
	{
		isFree = true;
		pool.Add(this);
		transform.DOScale(0.0f, board.releaseDuration)
			.SetEase(board.releaseEase, board.releaseOvershot)
			.OnComplete(delegate 
			{
				if (callback != null)
					callback(); /*gameObject.SetActive(false);*/
			});
	}


	internal static void addCandies(Action callback)
	{
		int i = 0;
		for (; i < pool.Count - 1; i++)
		{
			pool[i].respawn();
		}
		pool[i].respawn(callback);
		pool.Clear();
	}


	private void respawn(Action callback = null)
	{
		candy = board.getRundomly();
		renderer.sprite = candy.mainSprite;
		isFree = false;
		var pos = transform.position;
		pos.y = board.spawnPositionY;
		transform.position = pos;

		transform.localScale = Vector3.one * scale;
		transform.DOMove(anchor, board.fallingDuration)
			.SetEase(board.fallingEase)
			.OnComplete(delegate
			{
				if (callback != null)
					callback();
			});
	}


	public override string ToString()
	{
		var msg = string.Format("cell: {0}, type: {1}, pos: {2}, ancor: {3}", cell, candy.type, transform.position, anchor);
		return msg;
	}


}
