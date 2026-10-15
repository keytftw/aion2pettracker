using PetTracker.Core;
namespace PetTracker.Tests;

public sealed class AutomaticCardHeightTests
{
    [Theory]
    [InlineData(0,1)][InlineData(1,1)][InlineData(2,2)][InlineData(3,3)]
    [InlineData(4,4)][InlineData(7,7)][InlineData(8,7)][InlineData(150,7)]
    public void FitsVisibleCardsWithOneEmptySlotAndSevenCardCap(int count,int slots)
        =>Assert.Equal(slots,OverlayHeight.Fit(count));
}
