
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
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Debug.LogError("There are more than one instances of Board type in the game!");
		}
	}
	#endregion


	public ParticleSystem ps;

	public float fallingDuration = 0.2f;
	public Ease fallingEase = Ease.Linear;
	public float swapDuration = 0.2f;
	public float releaseDuration = 0.2f;
	public float releaseOvershot = 2.5f;
	public Ease releaseEase = Ease.Linear;


	[SerializeField]
	new Camera camera;

	[SerializeField]
	Texture2D[] boardPatterns;

	[SerializeField, Tooltip("Must be on 2 smaller than texture pattern")]
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


	public RectTransform roof;
	public RectTransform mapSelector;
	public RectTransform playButton;

	[SerializeField]
	Text scoreText;

	float score;
	float scoreToShow;

	Coroutine takeScoreC;


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

			if (x < cols && x >= 0 && y < rows && y >= 0)
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

	Stack<CandyCell> stack = new Stack<CandyCell>();



	void Start()
	{
		takePoints(PlayerPrefs.GetInt("_Score", 0));

		candies = new CandyCell[cols, rows];

		for (int i = 0; i < cols * rows; i++)
		{
			stack.Push(Instantiate(originalCandy.GetComponent<CandyCell>()));
		}
	}


	public void StartSession()
	{
		mapSelector.DOAnchorPos(mapSelector.anchoredPosition + Vector2.up * Screen.height, 0.1f);
		Vector2 center = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height) * 0.5f);
		Vector2 offset = center - (size - Vector2.one) * 0.5f * padding + (Vector2)transform.position;

		playButton.DOAnchorPos(playButton.anchoredPosition + Vector2.down * 250, 0.5f);
		playButton.DORotate(Vector3.forward * 90, 0.5f);

		roof.DOSizeDelta(roof.sizeDelta - Vector2.up * 1600, 1.0f)
			.SetEase(Ease.OutBounce)
			.OnComplete(delegate
			{
				Color[] pixels;
				if (unpackPattern(out pixels))
				{
					StartCoroutine(fillBoardUsePattern(pixels, offset, stack));
				}
				else
				{
					StartCoroutine(fillBoard(offset, stack));
				}
			});
	}


	public void takePoints(int addScore)
	{
		if (takeScoreC != null)
		{
			scoreToShow = score;
			StopCoroutine(takeScoreC);
		}
		score += addScore;
		PlayerPrefs.SetInt("_Score", (int)score);
		PlayerPrefs.Save();
		takeScoreC = StartCoroutine(takeScore(scoreToShow + addScore));
	}


	private IEnumerator takeScore(float sum)
	{
		while (scoreToShow != sum)
		{
			scoreToShow++;
			scoreText.text = string.Format("Score: {0}", (int)scoreToShow);
			yield return new WaitForSeconds(0.001f);
		}
	}


	#region Fill Board
	bool unpackPattern(out Color[] pattern)
	{
		pattern = null;
		Texture2D tex = boardPatterns[MapSelector.currentMap];
		if (tex.width > cols + 1 && tex.height > rows + 1)
		{
			Vector2 texOffset = new Vector2((tex.width - rows), (tex.height - cols)) * 0.5f;
			pattern = tex.GetPixels((int)texOffset.x, (int)texOffset.y, rows, cols);
			return true;
		}
		Debug.LogWarningFormat("Size of board must to be less that texture size for 2 in each axis, texture size ({0}, {1}) <= board size {2}", tex.width, tex.height, size);
		return false;
	}


	IEnumerator fillBoardUsePattern(Color[] pattern, Vector2 offset, Stack<CandyCell> stack)
	{
		WaitForSeconds wait = new WaitForSeconds(individualSpawnDelay);

		Vector2 cell = Vector2.zero;

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				if (pattern[i * (int)size.y + j].r > 0)
				{
					cell.x = j;
					cell.y = i;

					Candy candy;
					do
					{
						candy = getRundomly();
					}
					while (
						this[cell + Vector2.down] == this[cell + Vector2.down * 2] && this[cell + Vector2.down] == candy.type ||
						this[cell + Vector2.left] == this[cell + Vector2.left * 2] && this[cell + Vector2.left] == candy.type);

					this[cell] = spawnField(offset + cell * padding, cell, candy, stack.Pop());

					//var pos = camera.WorldToScreenPoint(offset + cell * padding);
					//this[cell].debText = Instantiate<Text>(debCellOrigin, pos, Quaternion.identity, deb.transform);

					yield return wait;
				}
			}
		}

		//foreach (var extraCell in stack.ToArray())
		//{
		//	Destroy(extraCell.gameObject);
		//}
		//stack.Clear();
	}


	IEnumerator fillBoard(Vector2 offset, Stack<CandyCell> stack)
	{
		WaitForSeconds wait = new WaitForSeconds(individualSpawnDelay);

		Vector2 cell = Vector2.zero;

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				cell.x = j;
				cell.y = i;
				this[cell] = spawnField(offset + cell * padding, cell, getRundomly(), stack.Pop());

				yield return wait;
			}
		}

		//foreach (var extraCell in stack.ToArray())
		//{
		//	Destroy(extraCell.gameObject);
		//}
		//stack.Clear();
	}


	Random rand = new Random(DateTime.Now.Millisecond);
	internal Candy getRundomly()
	{
		return candyTypes[rand.Next(candyTypes.Length)];
	}


	public float spawnPositionY
	{
		get;
		protected set;
	}

	CandyCell spawnField(Vector3 pos, Vector2 cell, Candy candy, CandyCell candyComponent)
	{
		var bck = Instantiate(originalTileBack, pos, Quaternion.identity, transform);
		bck.transform.localScale = Vector3.zero;
		bck.transform.DOScale(fieldScale, 0.2f).SetEase(Ease.OutBack);

		var spawnPos = pos;
		spawnPositionY = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height)).y + 1.0f;
		spawnPos.y = spawnPositionY;

		return candyComponent.init(cell, pos, spawnPos, candy, transform, candiesScale);
	}
	#endregion


	internal void OnCandyUnfocused(Vector2 cell)
	{
		focusedCandy = -Vector2.one;
	}


	internal void OnCandyFocused(Vector2 cell)
	{
		focusedCandy = cell;
	}


	internal void OnCandySelected(Vector2 overCandy)
	{
		if (focusedCandy != -Vector2.one)
		{
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
		}
	}


	private void findMatches()
	{
		Vector2 cell = Vector2.zero;
		List<CandyCell> candiesToDelete = new List<CandyCell>();

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				cell.x = j;
				cell.y = i;

				if(this[cell] != null && !candiesToDelete.Contains(this[cell]))
				{
					candiesToDelete.AddRange(findMatchFor(cell));
				}
			}
		}

		if(candiesToDelete.Count > 0)
		{
			CandyCell.release(candiesToDelete, fallCandiesDown);
		}
	}


	private void findMatchesLocally(Vector2 fc, Vector2 oc)
	{
		var candiesToDelete = findMatchFor(fc);
		candiesToDelete.AddRange(findMatchFor(oc));

		CandyCell.release(candiesToDelete, fallCandiesDown);
	}

	
	private void fallCandiesDown()
	{
		Vector2 cell = Vector2.zero;
		for (int j = 0; j < cols; j++)
		{
			cell.x = j;
			cell.y = 0;
			fallColumnDown(cell);
		}

		CandyCell.addCandies(findMatches);
	}


	private void fallColumnDown(Vector2 cell)
	{
		if( cell.y == rows )
		{
			return;
		}
		//Debug.LogFormat("Current cell: {0} = {1}", cell, this[cell]);

		if ( this[cell] == null || CandyCell.isNotReleased(this[cell]) )
		{
			fallColumnDown(cell + Vector2.up);
			return;
		}

		//Debug.LogFormat("To swap cell: {0} = {1}", cell, CandyCell.isNotReleased(this[cell]));

		var current = cell;
		do
		{
			cell += Vector2.up;
		}
		while (cell.y < rows && (this[cell] == null || CandyCell.isReleased(this[cell]) ) );

		if (this[cell] != null)
		{
			//Debug.LogFormat("Swap cell:\n {0} = {1}\n {2} = {3}\n", current, this[current], cell, this[cell]);
			fallingSwap(current, cell, null);
			fallColumnDown(current + Vector2.up);
		}
	}


	private List<CandyCell> findMatchFor(Vector2 cell)
	{
		//Debug.Log("Look for: " + cell.ToString("N"));

		Vector2 pCell = cell;
		var candy = this[cell];

		// horizontal
		List<CandyCell> hseq = new List<CandyCell>();

		lookForMatch(candy, cell + Vector2.left, Vector2.right, hseq);
		lookForMatch(candy, cell, Vector2.left, hseq);

		//Debug.Log("h : " + hseq.Count + " of type " + candy.candy.type);

		if (hseq.Count < 3)
		{
			hseq.Clear();
		}

		// vertical
		List<CandyCell> vseq = new List<CandyCell>();

		lookForMatch(candy, cell, Vector2.up, vseq);
		lookForMatch(candy, cell, Vector2.down, vseq);

		//Debug.Log("v : " + vseq.Count + " of type " + candy.candy.type);

		if (vseq.Count < 2)
		{
			vseq.Clear();
		}
		else if(hseq.Count == 0)
		{
			vseq.Add(candy);
		}

		vseq.AddRange(hseq);

		//Debug.Log(vseq.Count + " of type " + candy.candy.type);

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
