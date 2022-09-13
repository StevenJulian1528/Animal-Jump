[System.Serializable]
public class playerData
{
    public int coin, highscore, selectedSkin;
    public int[,] shopItem = { { 0, 0, 50 }, { 1, 1, 0 }, { 2, 0, 20 }, { 3, 0, 30 } };

    public playerData(Player player)
    {
        coin = player.m_coin;
        highscore = player.m_highScore;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                shopItem[i, j] = player.m_shopItem[i, j];
            }
        }
        selectedSkin = player.m_skin;
    }
}
