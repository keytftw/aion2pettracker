using PetTracker.Core;
namespace PetTracker.Tests;
public class PetPickerQueryTests
{
    private readonly PetCatalog catalog=new([new(1,"Ashen Spider","Fera"),new(2,"Angry Kalgolem","Fera"),new(3,"Unknown baseline","Fera")]);
    private static long? Total(int id)=>id switch{1=>104,2=>105,_=>null};
    [Fact] public void UnknownAnd104RemainIncompleteWhile105IsMaxed()
    {Assert.Equal([1,3],PetPickerQuery.Filter(catalog,"",false,Total).Select(p=>p.Id));Assert.Equal([2],PetPickerQuery.Filter(catalog,"",true,Total).Select(p=>p.Id));}
    [Theory] [InlineData("  ANGRY ",true,2)] [InlineData("2",true,2)] [InlineData("spider",false,1)]
    public void SearchCombinesWithSelectedTab(string query,bool maxed,int expected)
    {Assert.Equal(expected,Assert.Single(PetPickerQuery.Filter(catalog,query,maxed,Total)).Id);}
    [Fact] public void MaxPetCannotLeakIntoIncompleteSearch()
    {Assert.Empty(PetPickerQuery.Filter(catalog,"Angry",false,Total));Assert.Empty(PetPickerQuery.Filter(catalog,"Spider",true,Total));}
}
