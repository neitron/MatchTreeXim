
using System;
using System.Collections;
using UnityEngine;

using Random = System.Random;



public class Board : MonoBehaviour
{


	[SerializeField]
	new Camera camera;

	[SerializeField]
	Texture2D boardPattern;

	[SerializeField, Tooltip("")]
	Vector2 size;

	[SerializeField]
	float padding;

	[SerializeField]
	GameObject originalCandy;

	[SerializeField]
	GameObject originalTileBack;

	[SerializeField]
	Sprite[] sprites;

	int rows { get { return (int)size.x; } }
	int cols { get { return (int)size.y; } }


	Candy[,] candies;


	void Start()
	{
		candies = new Candy[cols, rows];

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
		Vector2 cell = Vector2.zero;
		Candy candy = originalCandy.GetComponent<Candy>();

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				if (pattern[i * 10 + j].r > 0)
				{
					cell.x = j;
					cell.y = i;
					candies[j, i] = spawnField(offset + cell * padding, sprites[rand.Next(sprites.Length)], candy);
					yield return new WaitForSeconds(0.1f);
				}
			}
		}
	}


	IEnumerator fillBoard(Vector2 offset, Random rand)
	{
		Vector2 cell = Vector2.zero;
		Candy candy = originalCandy.GetComponent<Candy>();

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				cell.x = j;
				cell.y = i;
				candies[j, i] = spawnField(offset + cell * padding, sprites[rand.Next(sprites.Length)], candy);
			}
			yield return new WaitForSeconds(0.1f);
		}
	}


	Candy spawnField(Vector3 pos, Sprite candy, Candy candyComponent)
	{
		Instantiate(originalTileBack, pos, Quaternion.identity, transform);
		var spawnPos = pos;
		spawnPos.y = camera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height)).y + 5.0f;
		 
		return Candy.create(candyComponent, pos, spawnPos, candy, transform);
	}


	void Update()
	{

	}
}
