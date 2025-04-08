using AIHackathon.DB.Models;
using BotCore.FilterRouter.Attributes;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Linq.Expressions;

namespace AIHackathon.Attributes
{
    public class IsRegisterFilterAttribute() : BaseFilterAttribute<User>(false)
    {
        public override Expression GetExpression(WriterExpression<User> writerExpression)
            => Expression.Not(Expression.PropertyOrField(Expression.PropertyOrField(writerExpression.ContextParameter, nameof(IUpdateContext<User>.User)), nameof(User.IsRegister)));
    }
}