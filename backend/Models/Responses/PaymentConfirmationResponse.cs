namespace ScreenProducerAPI.Models.Responses
{
    public class PaymentConfirmationResponse
    {
        public bool Success { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public decimal AmountReceived { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal RemainingBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsFullyPaid { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
