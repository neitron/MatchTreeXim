using UnityEngine;



[CreateAssetMenu(fileName = "New Candy", menuName = "Candies/Candy")]
public class Candy : ScriptableObject
{
	[SerializeField]
	Sprite[] sprite = new Sprite[3];

	[SerializeField]
	int id = -1;

	public Sprite mainSprite { get { if (sprite.Length > 1) return sprite[0]; else return null; } }
	public Sprite hSprite { get { if (sprite.Length > 2) return sprite[1]; else return null; } }
	public Sprite vSprite { get { if (sprite.Length > 3) return sprite[2]; else return null; } }

	public int type { get { return id; } }


}
