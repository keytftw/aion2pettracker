namespace PetTracker.Core;

public static class OverlayHeight
{
    public const int MinimumCards=1,MaximumCards=7,DefaultCards=3;
    public static int Clamp(int cards)=>Math.Clamp(cards,MinimumCards,MaximumCards);
    public static int Fit(int visiblePets)=>Clamp(visiblePets);
    public static int Pixels(int cards)=>(int)Math.Round((126+71*Clamp(cards))*4d/3);
    public static int CardsFromPixels(int pixels)=>Clamp((int)Math.Round((pixels*3d/4-126)/71));
    public static int Grow(int cards,int visiblePets)=>Clamp(Math.Max(cards,visiblePets));
}
