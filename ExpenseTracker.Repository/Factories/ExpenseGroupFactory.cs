using ExpenseTracker.Repository.Entities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ExpenseTracker.Repository.Helpers;

namespace ExpenseTracker.Repository.Factories
{
    public class ExpenseGroupFactory
    {
        ExpenseFactory expenseFactory = new ExpenseFactory();

        public ExpenseGroupFactory()
        {

        }

        public ExpenseGroup CreateExpenseGroup(DTO.ExpenseGroup expenseGroup)
        {
            return new ExpenseGroup()
            {
                Description = expenseGroup.Description,
                ExpenseGroupStatusId = expenseGroup.ExpenseGroupStatusId,
                Id = expenseGroup.Id,
                Title = expenseGroup.Title,
                UserId = expenseGroup.UserId,
                Expenses = expenseGroup.Expenses == null ? new List<Expense>() : expenseGroup.Expenses.Select(e => expenseFactory.CreateExpense(e)).ToList()
            };
        }

        public DTO.ExpenseGroup CreateExpenseGroup(ExpenseGroup expenseGroup)
        {
            return new DTO.ExpenseGroup()
            {
                Description = expenseGroup.Description,
                ExpenseGroupStatusId = expenseGroup.ExpenseGroupStatusId,
                Id = expenseGroup.Id,
                Title = expenseGroup.Title,
                UserId = expenseGroup.UserId,
                Expenses = expenseGroup.Expenses.Select(e => expenseFactory.CreateExpense(e)).ToList()
            };
        }

        public object CreateDataShapedObject(ExpenseGroup expenseGroup, List<string> fieldList)
        {
            return CreateDataShapedObject(CreateExpenseGroup(expenseGroup), fieldList);
        }

        public object CreateDataShapedObject(DTO.ExpenseGroup expenseGroup, List<string> fieldList)
        {
            List<string> workingFieldList = new List<string>(fieldList);

            if (!workingFieldList.Any())
            {
                return expenseGroup;
            }
            else
            {
                var expenseFieldList = workingFieldList.Where(f => f.ToLower().Contains("expense")).ToList();
                bool returnPartialExpense = expenseFieldList.Any() && !expenseFieldList.Contains("expenses");

                if (returnPartialExpense)
                {
                    workingFieldList.RemoveRange(expenseFieldList);
                    expenseFieldList = expenseFieldList.Select(f => f.Substring(f.IndexOf(".") + 1)).ToList();
                }
                else
                {
                    expenseFieldList.Remove("expenses");
                    workingFieldList.RemoveRange(expenseFieldList);
                }

                ExpandoObject shapedExpenseGroup = new ExpandoObject();

                foreach (var field in workingFieldList)
                {
                    var fieldValue = expenseGroup
                        .GetType()
                        .GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                        .GetValue(expenseGroup, null);

                    ((IDictionary<String, Object>) shapedExpenseGroup).Add(field, fieldValue);
                }

                if (returnPartialExpense)
                {
                    List<object> expenses = new List<object>();
                    foreach (var expense in expenseGroup.Expenses)
                    {
                        expenses.Add(expenseFactory.CreateDataShapedObject(expense, expenseFieldList));
                    }

                    ((IDictionary<String, Object>) shapedExpenseGroup).Add("expenses", expenses);
                }

                return shapedExpenseGroup;
            }
        }
    }
}
