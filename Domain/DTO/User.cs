using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("userID")]
        public int UserID { get; set; }

        [Column("chatID")]
        public long? ChatId { get; set; }
        [Column("phonenumber")]
        public string PhoneNumber { get; set; }
        [Column("isauthorized")]
        public bool IsAuthorized { get; set; }

        [Column("lastquestionid")]
        public int LastQuestionId { get; set; }
        [Column ("rating")]
        public int? Rating { get; set; }
    }
}
