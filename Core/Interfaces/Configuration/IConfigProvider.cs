namespace MOUSE.Core.Interfaces.Configuration
{
    public interface IConfigProvider<out TConfig>
        where TConfig : class
    {
        TConfig Config { get; }
    }
}