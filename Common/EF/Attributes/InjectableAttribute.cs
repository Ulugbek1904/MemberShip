namespace Common.EF.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class InjectableAttribute : Attribute
{
    public bool WithoutInterface { get; set; }

    public bool IsSingleton { get; set; }
}
