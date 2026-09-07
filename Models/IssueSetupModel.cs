using System.Data;

namespace IPOWeb.Models
{
    public class IssueSetupModel
    {
        public int mastergid { get; set; }
        public string mastercode { get; set; }
        public string mastername { get; set; }

        public string dependvalue { get; set; }

    }

    public class Qcdgridread
    {
        public string in_user_code { get; set; }
        public string in_master_code { get; set; }
    }

    public class OfferHeaderModel
    {
        public string? action { get; set; }
        public string? offer_header_gid { get; set; }
        public string? offer_code { get; set; }
        public string? offer_type { get; set; }
        public string? offer_listing_no { get; set; }
        public string? offer_isin { get; set; }
        public string? offer_status { get; set; }
        public string? offer_empdiscount { get; set; }
        public string? offer_remarks { get; set; }
        public string? client_code { get; set; }
        public string? user_code { get; set; }
        public string? role_code { get; set; }
        public string? active_status { get; set; }
        public string? ipo_status { get; set; }
        public char delete_flag { get; set; }
        public string? boa_checked_status { get; set; }

    }

    public class OfferDetailsModel
    {
        public int? offer_detail_gid { get; set; }
        public decimal? offer_precapital { get; set; } = 0;
        public int offer_issuesize { get; set; } = 0;
        public decimal offer_postcapital { get; set; } = 0;
        public int? offer_lotsize { get; set; }
        public decimal? offer_facevalue { get; set; } = 0;
        public decimal? offer_premiun { get; set; } = 0;
        public string? offer_pricetype { get; set; }
        public decimal? offer_fixedprice { get; set; } = 0;
        public decimal? offer_maximumprice { get; set; } = 0;
        public decimal? offer_minimumprice { get; set; } = 0;
        public decimal? offer_cutoffprice { get; set; } = 0;
        public string? offer_code { get; set; }
        public string? client_code { get; set; }
        public string? user_code { get; set; }
        public string? action { get; set; }
        public int ratio_numerator { get; set; }
        public int ratio_denominator { get; set; }
        public DateOnly? record_cutoff_date { get; set; }
    }

    public class OfferBankerModel
    {
        public string? action { get; set; }
        public int banker_gid { get; set; }
        public string? banker_type { get; set; }
        public string? banker_name { get; set; }
        public string? banker_holdname { get; set; }
        public string? banker_address { get; set; }
        public string? banker_city { get; set; }
        public string? banker_state { get; set; }
        public string? banker_pincode { get; set; }
        public string? banker_accountno { get; set; }
        public string? banker_ifsc { get; set; }
        public string? offer_code { get; set; }
        public string? client_code { get; set; }
        public string? user_code { get; set; }
    }
    public class OfferStackModel
    {
        public string? action { get; set; }
        public int? stack_gid { get; set; }
        public string? stack_code { get; set; }
        public string? stack_type { get; set; }
        public string? stack_name { get; set; }
        public string? stack_address { get; set; }
        public string? stack_city { get; set; }
        public string? stack_state { get; set; }
        public string? stack_pincode { get; set; }
        public string? stack_contact { get; set; }
        public string? stack_designation { get; set; }
        public string? stack_email { get; set; }
        public string? stack_mobile { get; set; }
        public string? offer_code { get; set; }
        public string? client_code { get; set; }
        public string? active_status { get; set; }
        public string? user_code { get; set; }

    }

    public class MilestoneModel
    {
        public string? action { get; set; }
        public int? milestone_gid { get; set; }
        public DateTime? offer_openingdate { get; set; }
        public DateTime? offer_closingdate { get; set; }
        public DateTime? offer_allotmentdate { get; set; }
        public DateTime? offer_approvaldate { get; set; }
        public DateTime? offer_listingdate { get; set; }
        public DateTime? offer_nsdldate { get; set; }
        public DateTime? offer_cdsldate { get; set; }
        public DateTime? offer_refunddate { get; set; }
        public string? offer_code { get; set; }
        public string? client_code { get; set; }
        public string? active_status { get; set; }
        public string? user_code { get; set; }
        public string? out_msg { get; set; }
        public int? out_result { get; set; }
    }

    public class CategoryModel
    {
        public string? action { get; set; }
        public string? json_data { get; set; }
        public string? offer_code { get; set; }
        public string? client_code { get; set; }
        public string? user_code { get; set; }
    }
}
