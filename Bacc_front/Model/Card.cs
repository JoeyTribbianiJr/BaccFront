
using System.Collections;

namespace Bacc_front
{
    /// <summary>
    /// 牌类
    /// </summary>
    public class Card
    {
        private Weight weight;
        private Suits color;
        private int pngName;

        public Card(string name, Weight weight, Suits color)
        {
            this.Weight = weight;
            this.Color = color;
            PngName = (int)Color * 13 + (int)Weight;
        }

        public int PngName { get => pngName; set => pngName = value; }
        public Suits Color { get => color; set => color = value; }
        public Weight Weight { get => weight; set => weight = value; }
    }
}
