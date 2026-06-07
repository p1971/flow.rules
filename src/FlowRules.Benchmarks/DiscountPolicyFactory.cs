using System.Threading.Tasks;

using FlowRules.Engine;
using FlowRules.Engine.Models;

namespace FlowRules.Benchmarks;

internal static class DiscountPolicyFactory
{
    private const string FailureMessage = "One or more adjust rules failed.";

    public static Policy<DiscountInput> Create() =>
        PolicyBuilder<DiscountInput>
            .Create()
            .WithId("Discount")
            .WithName("Discount")
            .WithRule(
                "GiveDiscount10",
                "GiveDiscount10",
                static (input, _) => ValueTask.FromResult(input.Customer.Country == "india"
                    && input.Customer.LoyaltyFactor <= 2
                    && input.Customer.TotalPurchasesToDate >= 5000
                    && input.OrderHistory.TotalOrders > 2
                    && input.VisitHistory.NoOfVisitsPerMonth > 2),
                failureMessage: static _ => FailureMessage)
            .WithRule(
                "GiveDiscount20",
                "GiveDiscount20",
                static (input, _) => ValueTask.FromResult(input.Customer.Country == "india"
                    && input.Customer.LoyaltyFactor == 3
                    && input.Customer.TotalPurchasesToDate >= 10000
                    && input.OrderHistory.TotalOrders > 2
                    && input.VisitHistory.NoOfVisitsPerMonth > 2),
                failureMessage: static _ => FailureMessage)
            .WithRule(
                "GiveDiscount25",
                "GiveDiscount25",
                static (input, _) => ValueTask.FromResult(input.Customer.Country != "india"
                    && input.Customer.LoyaltyFactor >= 2
                    && input.Customer.TotalPurchasesToDate >= 10000
                    && input.OrderHistory.TotalOrders > 2
                    && input.VisitHistory.NoOfVisitsPerMonth > 5),
                failureMessage: static _ => FailureMessage)
            .WithRule(
                "GiveDiscount30",
                "GiveDiscount30",
                static (input, _) => ValueTask.FromResult(input.Customer.LoyaltyFactor > 3
                    && input.Customer.TotalPurchasesToDate >= 50000
                    && input.Customer.TotalPurchasesToDate <= 100000
                    && input.OrderHistory.TotalOrders > 5
                    && input.VisitHistory.NoOfVisitsPerMonth > 15),
                failureMessage: static _ => FailureMessage)
            .WithRule(
                "GiveDiscount30NestedOrExample",
                "GiveDiscount30NestedOrExample",
                static (input, _) => ValueTask.FromResult((input.Customer.LoyaltyFactor > 3
                    && input.Customer.TotalPurchasesToDate >= 50000
                    && input.Customer.TotalPurchasesToDate <= 100000)
                    || input.OrderHistory.TotalOrders > 15),
                failureMessage: static _ => FailureMessage)
            .WithRule(
                "GiveDiscount35NestedAndExample",
                "GiveDiscount35NestedAndExample",
                static (input, _) => ValueTask.FromResult(input.Customer.LoyaltyFactor > 3
                    && input.Customer.TotalPurchasesToDate >= 100000
                    && input.OrderHistory.TotalOrders > 15
                    && input.VisitHistory.NoOfVisitsPerMonth > 25),
                failureMessage: static _ => FailureMessage)
            .Build();
}
