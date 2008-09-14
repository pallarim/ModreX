using System.Collections.Generic;

namespace ModularRex.RexNetwork
{
    public interface IClientRexFaceExpression
    {
        event RexFaceExpressionDelegate OnRexFaceExpression;
        void SendRexFaceExpression(List<string> expressionData);
    }
}