using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Factories;
using ExpenseTracker.API.Helpers;
using Marvin.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web;

namespace ExpenseTracker.API.Controllers
{
    [RoutePrefix("api")]
    public class ExpenseGroupsController : ApiController
    {
        IExpenseTrackerRepository _repository;
        ExpenseGroupFactory _expenseGroupFactory = new ExpenseGroupFactory();
        const int maxPageSize = 10;

        public ExpenseGroupsController()
        {
            _repository = new ExpenseTrackerEFRepository(new Repository.Entities.ExpenseTrackerContext());
        }

        public ExpenseGroupsController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [Route("expensegroups", Name = "ExpenseGroupsList")]
        [HttpGet]
        public IHttpActionResult Get(
            string sort = "id",
            string status = null,
            string userId = null,
            string fields = "",
            int page = 0,
            int pageSize = 5
        )
        {
            try
            {
                bool includeExpenses = false;
                var fieldList = fields.ToLower().Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (fieldList != null)
                {
                    includeExpenses = fieldList.Any(f => f.Contains("expense"));
                }

                int statusId = -1;
                if (status != null)
                {
                    switch (status.ToLower())
                    {
                        case "open": statusId = 1; break;
                        case "confirmed": statusId = 2; break;
                        case "processed": statusId = 3; break;
                        default: break;
                    }
                }

                IQueryable<Repository.Entities.ExpenseGroup> expenseGroups = null;

                if (includeExpenses)
                {
                    expenseGroups = _repository.GetExpenseGroupsWithExpenses();
                }
                else
                {
                    expenseGroups = _repository.GetExpenseGroups();
                }

                expenseGroups = expenseGroups
                    .Where(eg => (statusId == -1 || eg.ExpenseGroupStatusId == statusId))
                    .Where(eg => (userId == null || eg.UserId == userId))
                    .ApplySort(sort);

                pageSize = Math.Min(pageSize, maxPageSize);
                var totalCount = expenseGroups.Count();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var urlHelper = new UrlHelper(Request);
                var previousPageLink = page > 0 ? urlHelper.Link("ExpenseGroupsList", new
                    {
                        page = page - 1,
                        pageSize,
                        sort,
                        fields,
                        status,
                        userId
                    }) : "";
                var nextPageLink = page < totalPages - 1 ? urlHelper.Link("ExpenseGroupsList", new
                    {
                        page = page + 1,
                        pageSize,
                        sort,
                        fields,
                        status,
                        userId
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

                var expenseGroupsResult = expenseGroups
                    .Skip(pageSize * (page))
                    .Take(pageSize)
                    .ToList()
                    .Select(eg => _expenseGroupFactory.CreateDataShapedObject(eg, fieldList));

                return Ok(expenseGroupsResult);
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroups/{id}")]
        [HttpGet]
        public IHttpActionResult Get(int id) {
            try
            {
                var expenseGroup = _repository.GetExpenseGroup(id);

                if (expenseGroup == null)
                {
                    return NotFound();
                }
                else
                {
                    return Ok(_expenseGroupFactory.CreateExpenseGroup(expenseGroup));
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroups")]
        [HttpPost]
        public IHttpActionResult Post([FromBody] DTO.ExpenseGroup expenseGroup)
        {
            try
            {
                if (expenseGroup == null)
                {
                    return BadRequest();
                }

                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                var result = _repository.InsertExpenseGroup(eg);

                if (result.Status == RepositoryActionStatus.Created)
                {
                    var createdExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);

                    return Created(Request.RequestUri + "/" + createdExpenseGroup.Id.ToString(), createdExpenseGroup);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroups/{id}")]
        [HttpPut]
        public IHttpActionResult Put(int id, [FromBody] DTO.ExpenseGroup expenseGroup)
        {
            try
            {
                if (expenseGroup == null)
                {
                    return BadRequest();
                }

                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);
                var result = _repository.UpdateExpenseGroup(eg);

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var updatedExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);

                    return Ok(updatedExpenseGroup);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroups/{id}")]
        [HttpPatch]
        public IHttpActionResult Patch(int id, [FromBody] JsonPatchDocument<DTO.ExpenseGroup> patchDocument)
        {
            try
            {
                if (patchDocument == null)
                {
                    return BadRequest();
                }

                var expenseGroup = _repository.GetExpenseGroup(id);
                if (expenseGroup == null)
                {
                    return NotFound();
                }

                var eg = _expenseGroupFactory.CreateExpenseGroup(expenseGroup);

                patchDocument.ApplyTo(eg);

                var result = _repository.UpdateExpenseGroup(_expenseGroupFactory.CreateExpenseGroup(eg));

                if (result.Status == RepositoryActionStatus.Updated)
                {
                    var patchedExpenseGroup = _expenseGroupFactory.CreateExpenseGroup(result.Entity);
                    return Ok(patchedExpenseGroup);
                }

                return BadRequest();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroups/{id}")]
        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var result = _repository.DeleteExpenseGroup(id);

                if (result.Status == RepositoryActionStatus.Deleted)
                {
                    return StatusCode(HttpStatusCode.NoContent);
                }
                else if (result.Status == RepositoryActionStatus.NotFound)
                {
                    return NotFound();
                }

                return BadRequest();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}

 