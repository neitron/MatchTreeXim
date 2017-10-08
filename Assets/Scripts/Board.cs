
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

using Random = System.Random;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Board : MonoBehaviour
{
	#region Singleton
	public static Board instance;

	private void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}
		else
		{
			Debug.LogError("There are more than one instances of Board type in the game!");
		}
	}
	#endregion



	[SerializeField]
	new Camera camera;

	[SerializeField]
	Texture2D boardPattern;

	[SerializeField, Tooltip("")]
	Vector2 size;

	[SerializeField]
	float padding;

	[SerializeField]
	float candiesScale;

	[SerializeField]
	float fieldScale;

	[SerializeField]
	float individualSpawnDelay;

	[SerializeField]
	GameObject originalCandy;

	[SerializeField]
	GameObject originalTileBack;

	[SerializeField]
	Candy[] candyTypes;

	int rows { get { return (int)size.x; } }
	int cols { get { return (int)size.y; } }

	CandyCell[,] candies;
	Vector2 focusedCandy = -Vector2.one;


	private CandyCell this[Vector2 cell]
	{
		get
		{
			int x = (int)cell.x;
			int y = (int)cell.y;

			if(x < cols && x >= 0 && y < rows && y >= 0)
			{
				return candies[x, y];
			}
			Debug.LogWarningFormat("Out of range x = {0}, y = {1}", x, y);
			return null;
		}
		set
		{
			int x = (int)cell.x;
			int y = (int)cell.y;

			if (x < cols && x >= 0 && y < rows && y >= 0)
			{
				candies[x, y] = value;
				return;
			}
			Debug.LogErrorFormat("Out of range x = {0}, y = {1}", x, y);
		}
	}


	public RectTransform deb;
	public Text debCellOrigin;

	void Start()
	{
		candies = new CandyCell[cols, rows];

		Random rand = new Random(DateTime.Now.Millisecond);
		Vector2 center = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height) * 0.5f);
		Vector2 offset = center - (size - Vector2.one) * 0.5f * padding;

		Color[] pixels;
		if (boardPattern != null && unpackPattern(out pixels))
		{
			StartCoroutine(fillBoardUsePattern(pixels, offset, rand));
		}
		else
		{
			StartCoroutine(fillBoard(offset, rand));
		}
	}


	#region Fill Board
	bool unpackPattern(out Color[] pattern)
	{
		pattern = null;

		if (boardPattern.width > cols + 1 && boardPattern.height > rows + 1)
		{
			Vector2 texOffset = new Vector2((boardPattern.width - rows), (boardPattern.height - cols)) * 0.5f;
			pattern = boardPattern.GetPixels((int)texOffset.x, (int)texOffset.y, rows, cols);
			return true;
		}
		Debug.LogWarningFormat("Size of board must to be less that texture size for 2 in each axis, texture size ({0}, {1}) <= board size {2}", boardPattern.width, boardPattern.height, size);
		return false;
	}


	IEnumerator fillBoardUsePattern(Color[] pattern, Vector2 offset, Random rand)
	{
		WaitForSeconds wait = new WaitForSeconds(individualSpawnDelay);

		Vector2 cell = Vector2.zero;
		CandyCell candy = originalCandy.GetComponent<CandyCell>();

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				if (pattern[i * (int)size.y + j].r > 0)
				{
					int index = rand.Next(candyTypes.Length);
					cell.x = j;
					cell.y = i;
					this[cell] = spawnField(offset + cell * padding, cell, candyTypes[index], candy);

					var pos = camera.WorldToScreenPoint(offset + cell * padding);
					this[cell].debText = Instantiate<Text>(debCellOrigin, pos, Quaternion.identity, deb.transform);

					yield return wait;
				}
			}
		}
	}


	IEnumerator fillBoard(Vector2 offset, Random rand)
	{
		WaitForSeconds wait = new WaitForSeconds(individualSpawnDelay);

		Vector2 cell = Vector2.zero;
		CandyCell candy = originalCandy.GetComponent<CandyCell>();

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				cell.x = j;
				cell.y = i;
				this[cell] = spawnField(offset + cell * padding, cell, candyTypes[rand.Next(candyTypes.Length)], candy);
			}
			yield return wait;
		}
	}


	CandyCell spawnField(Vector3 pos, Vector2 cell, Candy candy, CandyCell candyComponent)
	{
		var bck = Instantiate(originalTileBack, pos, Quaternion.identity, transform);
		bck.transform.localScale = Vector3.zero;
		bck.transform.DOScale(fieldScale, 0.2f).SetEase(Ease.OutBack);

		var spawnPos = pos;
		spawnPos.y = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height)).y + 5.0f;

		return CandyCell.create(candyComponent, cell, pos, spawnPos, candy, transform, candiesScale);
	}
	#endregion


	internal void OnCandyUnfocused(Vector2 cell)
	{
		focusedCandy = -Vector2.one;

		updateDebInfo();
	}


	internal void OnCandyFocused(Vector2 cell)
	{
		focusedCandy = cell;
		Debug.Log("Focused " + cell);

		updateDebInfo();
	}


	internal void OnCandySelected(Vector2 overCandy)
	{
		if (focusedCandy != -Vector2.one)
		{
			Debug.Log("Over " + overCandy);

			var dir = overCandy - focusedCandy;
			
			// prevent diagonals
			if (Mathf.Abs(dir.x + dir.y) != 1)
			{
				focusedCandy = -Vector2.one;
				return;
			}

			if (isSwapAvalible(dir, overCandy))
			{
				var fc = focusedCandy;
				var oc = overCandy;
				swap(focusedCandy, overCandy, delegate
				{ findMatchesLocally(fc, oc); });
			}
			else
			{
				failSwap(this[focusedCandy], this[overCandy]);
			}

			focusedCandy = -Vector2.one;

			updateDebInfo();
		}
	}


	List<Vector2> pool = new List<Vector2>();
	private void findMatchesLocally(Vector2 fc, Vector2 oc)
	{
		findMatchFor(fc).ForEach(item => item.Despawn(pool));
		findMatchFor(oc).ForEach(item => item.Despawn(pool));

		Vector2 cell = Vector2.zero;
		//for (int i = rows - 1; i >= 0; i--)
		{
			//for (int j = 0; j < cols; j++)
			//{
			//	cell.x = j;
			//	cell.y = rows - 1;

			//	fallDown(cell, delegate
			//	{ fallDown(cell, null); });
			//}
		}


		for (int j = 0; j < cols; j++)
		{
			for (int i = 0; i < rows; i++)
			{
				cell.x = j;
				cell.y = i;
				if(CandyCell.isAvalible(this[cell]))
				{
					fallDown(cell);
					break; // GO TO NEXR COLUMN
				}
			}
		}
	}


	private void fallDown(Vector2 cell)
	{
		var current = cell;
		do
		{
			cell += Vector2.up;
		} while (cell.y < rows && ( this[cell] == null || CandyCell.isAvalible(this[cell])));
		
		fallingSwap(current, cell, delegate { fallDown(current + Vector2.up); });
	}


	private List<CandyCell> findMatchFor(Vector2 cell)
	{
		Debug.Log("Look for: " + cell.ToString("N"));

		Vector2 pCell = cell;
		var candy = this[cell];

		// horizontal
		List<CandyCell> hseq = new List<CandyCell>();

		lookForMatch(candy, cell + Vector2.left, Vector2.right, hseq);
		lookForMatch(candy, cell, Vector2.left, hseq);

		Debug.Log("h : " + hseq.Count + " of type " + candy.candy.type);

		if (hseq.Count < 3)
		{
			hseq.Clear();
		}

		// vertical
		List<CandyCell> vseq = new List<CandyCell>();

		lookForMatch(candy, cell, Vector2.up, vseq);
		lookForMatch(candy, cell, Vector2.down, vseq);

		Debug.Log("v : " + vseq.Count + " of type " + candy.candy.type);

		if (vseq.Count < 2)
		{
			vseq.Clear();
		}
		else if(hseq.Count == 0)
		{
			vseq.Add(candy);
		}

		vseq.AddRange(hseq);

		Debug.Log(vseq.Count + " of type " + candy.candy.type);

		if(vseq.Count <= 2)
		{
			vseq.Clear();
		}
		return vseq;
	}


	private void lookForMatch(CandyCell origin, Vector2 cell, Vector2 dir, List<CandyCell> seq)
	{
		Vector2 pCell = cell + dir;
		while (origin == this[pCell])
		{
			seq.Add(this[pCell]);
			pCell += dir;
		}
	}


	void updateDebInfo()
	{
		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				if(candies[j, i] != null)
				{
					candies[j, i].updateDebInfo(camera);
				}
			}
		}
	}


	private bool isSwapAvalible(Vector2 dir, Vector2 overCandy)
	{
		// x - focused, o - over, c - potential, n - others

		// linear for all directions	// cross for all directions
		//   x    						//   c        
		// x o c c						// x o x
		//   x  						//   c       
		if (linearValidation(dir, overCandy, focusedCandy) || crossValidation(dir, overCandy, focusedCandy))
		{
			return true;
		}

		if(linearValidation(-dir, focusedCandy, overCandy) || crossValidation(-dir, focusedCandy, overCandy))
		{
			return true;
		}

		return false;
	}


	private bool linearValidation(Vector2 dir, Vector2 overCandy, Vector2 focusedCandy)
	{
		Vector2 reflectedDir = new Vector2(dir.y, dir.x);

		return
			this[overCandy + dir] == this[focusedCandy] && this[overCandy + dir * 2] == this[focusedCandy] ||
			this[overCandy + reflectedDir] == this[focusedCandy] && this[overCandy + reflectedDir * 2] == this[focusedCandy] ||
			this[overCandy - reflectedDir] == this[focusedCandy] && this[overCandy - reflectedDir * 2] == this[focusedCandy];
	}


	private bool crossValidation(Vector2 dir, Vector2 overCandy, Vector2 focusedCandy)
	{
		Vector2 reflectedDir = new Vector2(dir.y, dir.x);

		return
			this[overCandy + reflectedDir] == this[focusedCandy] && this[overCandy - reflectedDir] == this[focusedCandy];
	}


	private void swap(Vector2 cell0, Vector2 cell1, Action callback)
	{
		CandyCell.swap(ref candies[(int)cell0.x, (int)cell0.y], ref candies[(int)cell1.x, (int)cell1.y], callback);
	}


	private void fallingSwap(Vector2 cell0, Vector2 cell1, Action callback)
	{
		CandyCell.fallingSwap(ref candies[(int)cell0.x, (int)cell0.y], ref candies[(int)cell1.x, (int)cell1.y], callback);
	}


	private void failSwap(CandyCell c0, CandyCell c1)
	{
		CandyCell.failSwap(c0, c1);
	}


}


public static class Vector2Ext
{
	public static readonly Vector2 downLeft = new Vector2(-1.0f, -1.0f);
	public static readonly Vector2 downRight = new Vector2(1.0f, -1.0f);
}
