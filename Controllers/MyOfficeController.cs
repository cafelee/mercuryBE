using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BackendExamApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyOfficeController : ControllerBase
    {
        private readonly IConfiguration _config;

        public MyOfficeController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("insert")]
        public async Task<IActionResult> Insert([FromBody] JsonElement inputJson)
        {
            string sid = "";
            string jsonWithSid = "";
            var connStr = _config.GetConnectionString("Default");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            //呼叫 NEWSID 預存程序產生新 SID
            using (var sidCmd = new SqlCommand("NEWSID", conn))
            {
                sidCmd.CommandType = System.Data.CommandType.StoredProcedure;
                sidCmd.Parameters.AddWithValue("@TableName", "MyOffice_ACPD");
                var sidParam = new SqlParameter("@ReturnSID", System.Data.SqlDbType.NVarChar, 20)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                sidCmd.Parameters.Add(sidParam);

                await sidCmd.ExecuteNonQueryAsync();
                sid = sidParam.Value?.ToString() ?? "";
            }

            //SID 加進原始 JSON
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(inputJson.ToString());
            dict["ACPD_SID"] = sid;
            jsonWithSid = JsonSerializer.Serialize(dict);

            //呼叫usp_Insert_MyOffice_ACPD
            using (var insertCmd = new SqlCommand("usp_Insert_MyOffice_ACPD", conn))
            {
                insertCmd.CommandType = System.Data.CommandType.StoredProcedure;
                insertCmd.Parameters.AddWithValue("@_JsonData", jsonWithSid);
                await insertCmd.ExecuteNonQueryAsync();
            }

            return Ok(new { status = "success", ACPD_SID = sid });
        }
    }
}
