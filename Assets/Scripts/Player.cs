namespace DefaultNamespace
{
    [System.Serializable]
    public class Player
    {
        public string Name;
        public int NumberOfClicks;
        public int ItemsInInventory;
        public int MatchesWon;

        public Player(string name, int numberOfClicks, int itemsInInventory, int matchesWon)
        {
            this.Name = name;
            this.NumberOfClicks = numberOfClicks;
            this.ItemsInInventory = itemsInInventory;
            this.MatchesWon = matchesWon;
        }
    }
}