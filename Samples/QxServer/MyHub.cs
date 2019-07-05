using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Qx;
using System.Linq;

namespace QxServer
{
    public class MyHub : QueryableHub<MyHub>
    {
        public MyHub(IAuthorizationService authorizationService, IAuthorizationPolicyProvider authorizationPolicyProvider)
            : base(authorizationService, authorizationPolicyProvider) { }

        [HubMethodName("Range")]
        public IAsyncQueryable<int> Range(int start, int count) => AsyncEnumerable.Range(start, count).AsAsyncQueryable();
    }

}
