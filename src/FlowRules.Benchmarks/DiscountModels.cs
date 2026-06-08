namespace FlowRules.Benchmarks;

public sealed record DiscountInput(
    DiscountCustomer Customer,
    DiscountOrderHistory OrderHistory,
    DiscountVisitHistory VisitHistory);

public sealed record DiscountCustomer(string Country, int LoyaltyFactor, int TotalPurchasesToDate);

public sealed record DiscountOrderHistory(int TotalOrders);

public sealed record DiscountVisitHistory(int NoOfVisitsPerMonth);
