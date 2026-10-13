namespace PetTracker.Core;

public static class CardScroll
{
    public static float Maximum(int pets,int slots,float stride)=>Math.Max(0,pets-Math.Max(1,slots))*stride;
    public static float Snap(float offset,int pets,int slots,float stride)=>
        Math.Clamp((float)Math.Round(offset/stride,MidpointRounding.AwayFromZero)*stride,0,Maximum(pets,slots,stride));
}
