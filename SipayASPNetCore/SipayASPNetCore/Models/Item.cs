namespace SipayASPNetCore.Models
{
    public class Item
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; }

        public string name { get { return this.Name; } }
        public string price { get { return Price.ToString("0.00").Replace(",", "."); } }
        public int qty { get { return this.Quantity; } }

        public int qnantity { get { return this.Quantity; } }
        public string description { get { return this.Description; } }
    }
}
