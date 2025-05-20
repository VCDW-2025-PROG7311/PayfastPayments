public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public string MerchantId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentStatus { get; set; }
}
