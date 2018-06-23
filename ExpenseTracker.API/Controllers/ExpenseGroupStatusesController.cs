using ExpenseTracker.Repository;
using ExpenseTracker.Repository.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ExpenseTracker.API.Controllers
{
    [RoutePrefix("api")]
    public class ExpenseGroupStatusesController : ApiController
    {
        IExpenseTrackerRepository _repository;
        ExpenseMasterDataFactory _expenseMasterDataFactory = new ExpenseMasterDataFactory();

        public ExpenseGroupStatusesController()
        {
            _repository = new ExpenseTrackerEFRepository(new Repository.Entities.ExpenseTrackerContext());
        }

        public ExpenseGroupStatusesController(IExpenseTrackerRepository repository)
        {
            _repository = repository;
        }

        [Route("expensegroupstatuses")]
        [HttpGet]
        public IHttpActionResult Get()
        {
            try
            {
                var expenseGroupStatuses = _repository.GetExpenseGroupStatusses()
                    .ToList()
                    .Select(egs => _expenseMasterDataFactory.CreateExpenseGroupStatus(egs));

                return Ok(expenseGroupStatuses);
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [Route("expensegroupstatuses/{id}")]
        [HttpGet]
        public IHttpActionResult Get(int id)
        {
            try
            {
                var expenseGroupStatus = _repository.GetExpenseGroupStatus(id);

                if (expenseGroupStatus == null)
                {
                    return NotFound();
                }

                return Ok(expenseGroupStatus);
            } catch (Exception e)
            {
                return InternalServerError(e);
            }
        }
    }
}
