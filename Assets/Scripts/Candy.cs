using UnityEngine;



[CreateAssetMenu(fileName = "New Candy", menuName = "Candies/Candy")]
public class Candy : ScriptableObject
{
	[SerializeField]
	Sprite[] sprite = new Sprite[3];

	[SerializeField]
	int id = -1;

	public Sprite mainSprite { get { return sprite[0]; } }

	public int type { get { return id; } }


}
