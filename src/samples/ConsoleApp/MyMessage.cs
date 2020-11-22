using System;

namespace ConsoleApp
{
    [Serializable]
    internal class MyMessage
    {
        public int Id { get; set; }
        public string? Text { get; set; }

        public override string ToString()
        {
            return $"\"{Text}\" (message ID = {Id})";
        }
    }
}