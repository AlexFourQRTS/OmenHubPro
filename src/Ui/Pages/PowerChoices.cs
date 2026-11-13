namespace OmenHub.Ui.Pages
{
    /// <summary>Captions for the fixed wattage choices of the power cards.</summary>
    public static class PowerChoices
    {
        public static string[] Captions(int[] watts)
        {
            var captions = new string[watts.Length];
            for (int index = 0; index < watts.Length; index++) captions[index] = watts[index] + " Вт";
            return captions;
        }
    }
}
