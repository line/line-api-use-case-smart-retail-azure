namespace LineApiUseCaseSmartRetail.Entities
{
    /// <summary>
    /// 商品
    /// </summary>
    public class Item : BaseEntity
    {
        public string Barcode { get; set; }
        public string ImageUrl { get; set; }
        public string ItemName { get; set; }
        public float ItemPrice { get; set; }
    }
}
