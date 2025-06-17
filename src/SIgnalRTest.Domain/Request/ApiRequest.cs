using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIgnalRTest.Domain.Request;
public record ApiRequest(HttpMethod method, string url, object? requestBody = default!, string? token = default!);
