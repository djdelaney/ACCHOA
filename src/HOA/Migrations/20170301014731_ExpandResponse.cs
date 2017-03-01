using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HOA.Migrations
{
    public partial class ExpandResponse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "Responses",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 512,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "Responses",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
