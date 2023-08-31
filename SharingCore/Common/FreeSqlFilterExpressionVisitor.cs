using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FreeSql.SharingCore.Common
{
    internal class FreeSqlFilterExpressionVisitor: ExpressionVisitor
    {
        private List<string> ConditionList = new List<string>();

        /// <summary>
        /// 解析表达式树 是否不设置过滤器
        /// </summary>
        /// <returns></returns>
        public bool IsNon()
        {
            return ConditionList.Count == 1 && ConditionList.First().ToLower() == "false";
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            ConditionList.Add(node.ToString());
            base.Visit(node.Right);
            base.Visit(node.Left);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            ConditionList.Add(node.ToString());
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            ConditionList.Add(node.ToString());
            return node;
        }
    }
}
