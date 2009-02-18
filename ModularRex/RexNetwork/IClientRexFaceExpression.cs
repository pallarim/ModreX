using System.Collections.Generic;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// This client supports realXtend-style facial animation
    /// </summary>
    public interface IClientRexFaceExpression
    {
        event RexGenericMessageDelegate OnRexFaceExpression;
        void SendRexFaceExpression(List<string> expressionData);
    }
}