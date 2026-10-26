# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PostgreSQL.Destruction` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.1.0] - 2026-10-26

### Added

- Schema destruction: `PostgreSqlDroppedSchema` wraps an `ISchema` and lazily drops its PostgreSQL storage with `DROP SCHEMA IF EXISTS … CASCADE` on first access of `Name`, `Tables` or `ForeignKeys`, removing the tables, rows, indexes and foreign keys created by `PostgreSqlCreatedSchema`. Dropping storage that does not exist is a no-op, and other schemas are left untouched.
