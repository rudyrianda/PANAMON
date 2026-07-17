using System;
using System.Data;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=localhost;Database=Panamon;Trusted_Connection=True;TrustServerCertificate=True;";
        string sql = @"
WITH ShiftData AS (
    SELECT
        CASE
            WHEN CAST(SDate AS TIME) < '07:00:00'
                THEN CAST(DATEADD(DAY, -1, SDate) AS DATE)
            ELSE CAST(SDate AS DATE)
        END AS ReportDate,
        SDate,
        TotalUnit,
        NoOfOperator,
        ShiftMode AS OriginalShiftMode,
        CASE 
            WHEN ShiftMode = 'NON-SHIFT' THEN
                CASE 
                    WHEN MONTH(CAST(DATEADD(hour, -7, SDate) AS DATE)) = 7 AND YEAR(CAST(DATEADD(hour, -7, SDate) AS DATE)) = 2026 AND DAY(CAST(DATEADD(hour, -7, SDate) AS DATE)) <= 5 THEN 'NON-SHIFT'
                    WHEN CAST(SDate AS TIME) >= '07:00:00' AND CAST(SDate AS TIME) <= '15:45:00' THEN 'SHIFT 1'
                    WHEN CAST(SDate AS TIME) > '15:45:00' AND CAST(SDate AS TIME) <= '18:00:00' THEN 'OVERTIME SHIFT 1'
                    WHEN CAST(SDate AS TIME) > '18:00:00' AND CAST(SDate AS TIME) <= '23:15:00' THEN 'OVERTIME SHIFT 3'
                    ELSE 'SHIFT 3'
                END
            WHEN ShiftMode = 'OVERTIME' THEN
                CASE 
                    WHEN MONTH(CAST(DATEADD(hour, -7, SDate) AS DATE)) = 7 AND YEAR(CAST(DATEADD(hour, -7, SDate) AS DATE)) = 2026 AND DAY(CAST(DATEADD(hour, -7, SDate) AS DATE)) <= 5 THEN 'OVERTIME'
                    WHEN CAST(SDate AS TIME) >= '15:45:00' AND CAST(SDate AS TIME) <= '18:00:00' THEN 'OVERTIME SHIFT 1'
                    WHEN CAST(SDate AS TIME) > '18:00:00' AND CAST(SDate AS TIME) <= '23:15:00' THEN 'OVERTIME SHIFT 3'
                    WHEN CAST(SDate AS TIME) > '23:15:00' OR CAST(SDate AS TIME) <= '07:00:00' THEN 'SHIFT 3'
                    ELSE 'OVERTIME'
                END
            WHEN ShiftMode = 'SHIFT 2' AND MONTH(CAST(DATEADD(hour, -7, SDate) AS DATE)) = 7 AND YEAR(CAST(DATEADD(hour, -7, SDate) AS DATE)) = 2026 THEN
                CASE 
                    WHEN CAST(SDate AS TIME) >= '07:00:00' AND CAST(SDate AS TIME) <= '15:45:00' THEN 'SHIFT 1'
                    WHEN CAST(SDate AS TIME) > '15:45:00' AND CAST(SDate AS TIME) <= '18:00:00' THEN 'OVERTIME SHIFT 1'
                    WHEN CAST(SDate AS TIME) > '18:00:00' AND CAST(SDate AS TIME) <= '23:15:00' THEN 'OVERTIME SHIFT 3'
                    ELSE 'SHIFT 3'
                END
            WHEN ShiftMode = 'SHIFT 3' AND CAST(SDate AS TIME) > '18:00:00' AND CAST(SDate AS TIME) <= '23:15:00' THEN 'OVERTIME SHIFT 3'
            ELSE ShiftMode
        END AS ShiftMode,
        MachineCode,
        LAG(SDate) OVER (PARTITION BY MachineCode ORDER BY SDate) AS PreviousSDate,
        LAG(TotalUnit) OVER (PARTITION BY MachineCode ORDER BY SDate) AS PreviousUnit,
        LAG(ShiftMode) OVER (PARTITION BY MachineCode ORDER BY SDate) AS PreviousShiftMode
    FROM oeesn
    WHERE (
        (YEAR(SDate) = 2026 AND MONTH(SDate) = 7
         AND CAST(SDate AS TIME) >= '07:00:00')
        OR
        (SDate >= '2026-07-02'
         AND SDate < '2026-08-02'
         AND CAST(SDate AS TIME) < '07:00:00')
    )
    AND MachineCode = 'MCH1-02'
),
ShiftDataFiltered AS (
    SELECT 
        ReportDate,
        SDate,
        MachineCode,
        OriginalShiftMode,
        CASE
            WHEN ShiftMode = 'OVERTIME'
                 AND CAST(SDate AS TIME) < '16:00:00'
                 AND PreviousShiftMode = 'NON-SHIFT'
            THEN 'NON-SHIFT'
            ELSE ShiftMode
        END AS ShiftMode,
        NoOfOperator,
        TotalUnit,
        PreviousUnit,
        CASE
            WHEN PreviousUnit IS NULL THEN TotalUnit
            WHEN TotalUnit < PreviousUnit THEN TotalUnit
            ELSE TotalUnit - PreviousUnit
        END AS DeltaUnit
    FROM ShiftData
)
SELECT 
    ReportDate,
    OriginalShiftMode,
    ShiftMode,
    SUM(DeltaUnit) as TotalDelta
FROM ShiftDataFiltered
WHERE ReportDate IN ('2026-07-14', '2026-07-15')
GROUP BY ReportDate, OriginalShiftMode, ShiftMode
ORDER BY ReportDate, ShiftMode;
";
        try {
            using (var conn = new SqlConnection(connStr)) {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn)) {
                    using (var reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            Console.WriteLine($"{reader["ReportDate"]:yyyy-MM-dd} | {reader["OriginalShiftMode"]} | {reader["ShiftMode"]} | Delta: {reader["TotalDelta"]}");
                        }
                    }
                }
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }
}
