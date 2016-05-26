using System.Threading.Tasks;

namespace MOUSE.Core.Misc
{
    public static class TaskExt
    {
        static readonly Task s_done = Task.FromResult((object)null);

        static public Task Done
        {
            get { return s_done; }
        }
    }
}