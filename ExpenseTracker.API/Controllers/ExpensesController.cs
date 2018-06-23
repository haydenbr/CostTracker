using ExpenseTracker.API.Helpers;
using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Factories;
using Marvin.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Routing;

namespace ExpenseTracker.API.Controllers
{
    [RoutePrefix("api")]
    public class ExpensesController : ApiController
    {
        IExpenseTrackerRepository _repository;
        ExpenseFactory _expenseFactory = new ExpenseFactory();
        const int maxPageSize = 10;

        public ExpensesController()
        {
            _repository = new ExpenseTrackerEFRepository(new Repository.Entities.ExpenseTrackerContext());
        }

        public ExpensesController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [Route("expenses", Name = "ExpenseList")]
        [HttpGet]
        public IHttpActionResult Get(
            string sort = "date",
            string fields = null,
            int page = 0,
            int pageSize = 5
        )
        {
            try
            {
                var expenses = _repository.GetExpenses().ApplySort(sort);

                pageSize = Math.Min(pageSize, maxPageSize);
                var totalCount = expenses.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var urlHelper = new UrlHelper(Request);
                var previousPageLink = page > 0 ? urlHelper.Link("ExpenseList", new
                {
                    page = page - 1,
                    pageSize,
                    sort
                }) : "";
                var nextPageLink = page < page - 1 ? urlHelper.Link("ExpenseList", new
                {
                    page = page + 1,
                    pageSize,
                    sort
                }) : "";
                var paginationHeader = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages,
                    previousPageLink,
                    nextPageLink
                };

                HttpContext.Current.Response.Headers.Add("x-pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationHeader));

                var expenseResults = expenses
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList()
                    .Select(e => _expenseFactory.CreateExpense(e));

                return Ok(expenseResults);
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expenses/{id}")]
        [Route("expensegroups/{expenseGroupId}/expenses/{id}")]
        [HttpGet]
        public IHttpActionResult Get(int id, int? expenseGroupId = null)
        {
            try
            {
                Repository.Entities.Expense expense = null;

                if (expenseGroupId == null)
                {
                    expense = _repository.GetExpense(id);
                } else
                {
                    var expensesInGroup = _repository.GetExpenses((int) expenseGroupId);

                    if (expensesInGroup != null)
                    {
                        expense = expensesInGroup.FirstOrDefault(e => e.Id == id);
                    } 
                }

                if (expense == null)
                {
                    return NotFound();
                } else
                {
                    return Ok(_expenseFactory.CreateExpense(expense));
                }
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expenses")]
        [HttpPost]
        public IHttpActionResult Post([FromBody] DTO.Expense expense)
        {
            try
            {
                if (expense == null)
                {
                    return BadRequest();
                }

                var e = _expenseFactory.CreateExpense(expense);
                var result = _repository.InsertExpense(e);

                if (result.Status == RepositoryActionStatus.Created)
                {
                    var createdExpense = _expenseFactory.CreateExpense(result.Entity);
                    return Created(Request.RequestUri + "/" + createdExpense.Id.ToString(), createdExpense);
                } else
                {
                    return BadRequest();
                }
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expenses{id}")]
        [HttpPut]
        public IHttpActionResult Put(int id, [FromBody] DTO.Expense expense)
        {
            try
            {
                if (expense == null)
                {
                    return BadRequest();
                }

                var e = _expenseFactory.CreateExpense(expense);
                var result = _repository.UpdateExpense(e);

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var updatedExpense = _expenseFactory.CreateExpense(result.Entity);

                    return Ok(updatedExpense);
                } else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                } else
                {
                    return BadRequest();
                }
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expenses{id}")]
        [HttpPatch]
        public IHttpActionResult Patch(int id, [FromBody] JsonPatchDocument<DTO.Expense> patchDocument)
        {
            try
            {
                if (patchDocument == null)
                {
                    return NotFound();
                }

                var expense = _repository.GetExpense(id);

                if (expense == null)
                {
                    return NotFound();
                }

                var e = _expenseFactory.CreateExpense(expense);
                patchDocument.ApplyTo(e);
                var result = _repository.UpdateExpense(_expenseFactory.CreateExpense(e));

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var patchedExpenseGroup = _expenseFactory.CreateExpense(result.Entity);
                    return Ok(patchedExpenseGroup);
                }

                return BadRequest();
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expenses{id}")]
        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var result = _repository.DeleteExpense(id);

                if (result.Status == RepositoryActionStatus.Deleted)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroups/{expenseGroupId}/expenses", Name = "ExpensesForGroup")]
        [HttpGet]
        public IHttpActionResult GetExpensesInExpenseGroup(
            int expenseGroupId,
            string sort = "date",
            string fields = "",
            int page = 0,
            int pageSize = 5
        ) {
            try
            {
                var expenses = _repository.GetExpenses(expenseGroupId).ApplySort(sort);

                if (expenses == null)
                {
                    return NotFound();
                }

                pageSize = Math.Min(pageSize, maxPageSize);
                var totalCount = expenses.Count();
                var totalPages = (int) Math.Ceiling((double) totalCount / pageSize);
                var urlHelper = new UrlHelper(Request);
                var previousPageLink = page > 0 ? urlHelper.Link("ExpensesForGroup", new
                    {
                        page = page - 1,
                        pageSize,
                        sort
                    }) : "";
                var nextPageLink = page < totalPages - 1 ? urlHelper.Link("ExpensesForGroup", new
                    {
                        page = page + 1,
                        pageSize,
                        sort
                    }) : "";
                var paginationHeader = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages,
                    previousPageLink,
                    nextPageLink
                };

                HttpContext.Current.Response.Headers.Add("x-pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationHeader));

                var fieldList = fields.ToLower().Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
                var expensesResult = expenses
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList()
                    .Select(e => _expenseFactory.CreateDataShapedObject(e, fieldList));

                return Ok(expensesResult);
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}
