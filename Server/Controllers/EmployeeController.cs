using equiavia.components.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace equiavia.components.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(ILogger<EmployeeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IList<Employee> Get()
        {
            return new List<Employee>
            {
                new Employee
                {
                    Id=1,
                    Name="CEO"
                },
                new Employee
                {
                    Id=2,
                    Name="CFO",
                    ParentId=1
                },
                new Employee
                {
                    Id=3,
                    Name="Accountant",
                    ParentId=2
                },
                new Employee
                {
                    Id=4,
                    Name="Executive Assistant",
                    ParentId=1
                },
                new Employee
                {
                    Id=5,
                    Name="Director",
                    ParentId=6
                },
                new Employee
                {
                    Id=6,
                    Name="Chair of the Board"
                },

            };
        }
    }
}
