using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Data;

/// <summary>
/// Creates demo users and permits on first run so the app has realistic data to show.
/// All demo users share the password: Password@123
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Password@123";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        User NewUser(string name, string email, UserRole role, string company)
        {
            var u = new User { FullName = name, Email = email, Role = role, Company = company };
            u.PasswordHash = hasher.HashPassword(u, DemoPassword);
            return u;
        }

        var requester = NewUser("Rahul Nair", "requester@ptwdemo.com", UserRole.Requester, "Gulf Mechanical Contracting");
        var requester2 = NewUser("Imran Sheikh", "requester2@ptwdemo.com", UserRole.Requester, "Al Doha Scaffolding");
        var areaAuthority = NewUser("Khalid Al-Marri", "areaauthority@ptwdemo.com", UserRole.AreaAuthority, "Operations - Train 4");
        var issuer = NewUser("John Mathew", "issuer@ptwdemo.com", UserRole.Issuer, "Operations - Permit Office");
        var admin = NewUser("System Admin", "admin@ptwdemo.com", UserRole.Admin, "IT");

        db.Users.AddRange(requester, requester2, areaAuthority, issuer, admin);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var year = now.Year;
        var seq = 0;

        Permit NewPermit(PermitType type, string title, string description, string location, string? tag,
            RiskLevel risk, User by, int startOffsetHours, int durationHours, string hazards, string precautions, string ppe, int workers)
        {
            seq++;
            return new Permit
            {
                PermitNumber = $"PTW-{year}-{seq:D4}",
                Type = type,
                Title = title,
                WorkDescription = description,
                Location = location,
                EquipmentTag = tag,
                RiskLevel = risk,
                RequestedById = by.Id,
                PlannedStart = now.AddHours(startOffsetHours),
                PlannedEnd = now.AddHours(startOffsetHours + durationHours),
                Hazards = hazards,
                Precautions = precautions,
                RequiredPpe = ppe,
                WorkersCount = workers,
                CreatedAt = now.AddHours(startOffsetHours - 24),
            };
        }

        void Log(Permit p, string action, PermitStatus? from, PermitStatus to, User by, DateTime at, string? comment = null) =>
            p.History.Add(new PermitHistory
            {
                Action = action, FromStatus = from, ToStatus = to, PerformedById = by.Id, Timestamp = at, Comment = comment
            });

        void Submit(Permit p, DateTime at) { p.Status = PermitStatus.Submitted; p.SubmittedAt = at; Log(p, "Submitted", PermitStatus.Draft, PermitStatus.Submitted, db.Users.Local.First(u => u.Id == p.RequestedById), at); }
        void Approve(Permit p, DateTime at) { p.Status = PermitStatus.Approved; p.ApprovedById = areaAuthority.Id; p.ApprovedAt = at; Log(p, "Approved", PermitStatus.Submitted, PermitStatus.Approved, areaAuthority, at, "Area walk-down completed. OK to proceed."); }
        void Issue(Permit p, DateTime at) { p.Status = PermitStatus.Active; p.IssuedById = issuer.Id; p.IssuedAt = at; Log(p, "Issued", PermitStatus.Approved, PermitStatus.Active, issuer, at, "Toolbox talk done, site conditions verified."); }

        // 1. Active hot work
        var p1 = NewPermit(PermitType.HotWork, "Welding on fire water line flange",
            "Cut and re-weld 6\" flange on fire water ring main near compressor K-401. Line drained and positively isolated.",
            "Train 4 - Compressor House", "FW-6-401", RiskLevel.High, requester, -2, 8,
            "Fire, hot surfaces, sparks, adjacent hydrocarbon equipment",
            "Fire watch assigned, fire blanket in place, extinguisher at site, drains covered, continuous gas monitoring",
            "Coverall, welding helmet, leather gloves, safety shoes, hard hat", 4);
        Log(p1, "Created", null, PermitStatus.Draft, requester, now.AddHours(-26));
        Submit(p1, now.AddHours(-20)); Approve(p1, now.AddHours(-6));
        p1.GasTests.Add(new GasTest { OxygenPercent = 20.9, LelPercent = 0, H2sPpm = 0, CoPpm = 0, IsPassed = true, TestedById = issuer.Id, TestedAt = now.AddHours(-2.5), Remarks = "Tested at flange and 3 m radius" });
        Issue(p1, now.AddHours(-2));

        // 2. Approved confined space entry - gas test is too old, so issue will ask for a retest
        var p2 = NewPermit(PermitType.ConfinedSpaceEntry, "Internal inspection of separator V-401",
            "Vessel entry for internal visual inspection and UT thickness measurement as per inspection plan.",
            "Train 4 - Separation Area", "V-401", RiskLevel.High, requester, 1, 10,
            "Oxygen deficiency, toxic gas (H2S), engulfment, restricted access",
            "Vessel purged and ventilated, standby man at manway, rescue plan briefed, blinds installed on all inlets",
            "Coverall, full body harness, H2S personal monitor, hard hat, safety shoes", 3);
        Log(p2, "Created", null, PermitStatus.Draft, requester, now.AddHours(-30));
        Submit(p2, now.AddHours(-28)); Approve(p2, now.AddHours(-5));
        p2.Isolations.Add(new Isolation { Type = IsolationType.Process, IsolationPoint = "XV-4011 inlet valve", LockNumber = "L-0231", Description = "Spade installed on inlet", AppliedById = issuer.Id, AppliedAt = now.AddHours(-4) });
        p2.Isolations.Add(new Isolation { Type = IsolationType.Electrical, IsolationPoint = "Agitator motor MCC-4 / F12", LockNumber = "L-0232", AppliedById = issuer.Id, AppliedAt = now.AddHours(-4) });
        p2.GasTests.Add(new GasTest { OxygenPercent = 20.8, LelPercent = 2, H2sPpm = 1, CoPpm = 3, IsPassed = true, TestedById = issuer.Id, TestedAt = now.AddHours(-3), Remarks = "Top, middle and bottom of vessel" });

        // 3. Submitted electrical work - waiting for Area Authority
        var p3 = NewPermit(PermitType.Electrical, "Replace MCC breaker for pump P-210B",
            "Replace faulty 63A breaker on MCC-2 feeder for standby pump P-210B.",
            "Substation SS-2", "MCC-2/F07", RiskLevel.Medium, requester, 3, 6,
            "Electrical shock, arc flash",
            "Breaker racked out and locked, voltage absence test, arc flash boundary marked",
            "Arc-rated coverall, insulated gloves, face shield, safety shoes", 2);
        Log(p3, "Created", null, PermitStatus.Draft, requester, now.AddHours(-3));
        Submit(p3, now.AddHours(-1));

        // 4. Draft work at height
        var p4 = NewPermit(PermitType.WorkAtHeight, "Insulation removal on pipe rack PR-3",
            "Remove damaged insulation at 8 m elevation for CUI inspection. Scaffold tagged green.",
            "Pipe Rack PR-3", null, RiskLevel.Medium, requester2, 24, 9,
            "Fall from height, dropped objects",
            "Green-tagged scaffold, 100% tie-off, barricade below, tool lanyards",
            "Full body harness, hard hat with chin strap, safety shoes, gloves", 3);
        Log(p4, "Created", null, PermitStatus.Draft, requester2, now.AddMinutes(-40));

        // 5. Closed cold work
        var p5 = NewPermit(PermitType.ColdWork, "Painting of utility water tank T-301 ladder",
            "Surface preparation (hand tools only) and painting of external ladder.",
            "Utilities Area", "T-301", RiskLevel.Low, requester2, -30, 8,
            "Paint fumes, slips",
            "Hand tools only, MSDS reviewed, area barricaded",
            "Coverall, goggles, respirator, gloves", 2);
        Log(p5, "Created", null, PermitStatus.Draft, requester2, now.AddHours(-60));
        Submit(p5, now.AddHours(-55)); Approve(p5, now.AddHours(-34)); Issue(p5, now.AddHours(-30));
        p5.Status = PermitStatus.Closed; p5.ClosedAt = now.AddHours(-22);
        Log(p5, "Closed", PermitStatus.Active, PermitStatus.Closed, issuer, now.AddHours(-22), "Job complete, area housekept.");

        // 6. Rejected excavation
        var p6 = NewPermit(PermitType.Excavation, "Trial pit for new cable route",
            "Manual excavation 1.5 m deep to locate existing cables before new cable route.",
            "Train 4 - Road 5 crossing", null, RiskLevel.High, requester, 20, 8,
            "Underground services, cave-in",
            "Cable detector survey, manual digging only",
            "Coverall, hard hat, safety shoes, gloves", 5);
        Log(p6, "Created", null, PermitStatus.Draft, requester, now.AddHours(-10));
        Submit(p6, now.AddHours(-9));
        p6.Status = PermitStatus.Rejected; p6.LastComment = "Underground services drawing not attached. Resubmit with latest as-built.";
        Log(p6, "Rejected", PermitStatus.Submitted, PermitStatus.Rejected, areaAuthority, now.AddHours(-7), p6.LastComment);

        // 7. Suspended hot work
        var p7 = NewPermit(PermitType.HotWork, "Grinding on tank T-102 access platform",
            "Grind and repair cracked weld on T-102 platform handrail.",
            "Tank Farm - T-102", "T-102", RiskLevel.High, requester2, -4, 8,
            "Fire, sparks near hydrocarbon storage",
            "Fire watch, spark screens, continuous gas monitor",
            "Coverall, face shield, leather gloves, hard hat", 2);
        Log(p7, "Created", null, PermitStatus.Draft, requester2, now.AddHours(-28));
        Submit(p7, now.AddHours(-26)); Approve(p7, now.AddHours(-6));
        p7.GasTests.Add(new GasTest { OxygenPercent = 20.9, LelPercent = 0, H2sPpm = 0, CoPpm = 2, IsPassed = true, TestedById = issuer.Id, TestedAt = now.AddHours(-4.5) });
        Issue(p7, now.AddHours(-4));
        p7.Status = PermitStatus.Suspended; p7.LastComment = "Gas alarm in adjacent tank T-103. All hot work stopped.";
        Log(p7, "Suspended", PermitStatus.Active, PermitStatus.Suspended, areaAuthority, now.AddHours(-1), p7.LastComment);

        // 8. Active cold work in the SAME area as permit 1 - shows the SIMOPS conflict check
        var p8 = NewPermit(PermitType.ColdWork, "Valve replacement on fire water header",
            "Replace 4\" gate valve on fire water header. Mechanical work only.",
            "Train 4 - Compressor House", "FW-4-412", RiskLevel.Medium, requester2, -1, 6,
            "Manual handling, pressurised line",
            "Line depressurised and drained, lifting plan in place",
            "Coverall, hard hat, safety shoes, gloves, goggles", 3);
        Log(p8, "Created", null, PermitStatus.Draft, requester2, now.AddHours(-20));
        Submit(p8, now.AddHours(-18)); Approve(p8, now.AddHours(-3));
        Issue(p8, now.AddHours(-1));

        db.Permits.AddRange(p1, p2, p3, p4, p5, p6, p7, p8);
        await db.SaveChangesAsync();
    }
}
