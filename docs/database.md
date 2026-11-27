# Database diagram

Generated from the real database by `python3 tools/db_diagram.py`. Do not edit by hand.

Reading the lines: `||--o{` = one to many; `|o--o{` = optional (nullable FK).
Types are PostgreSQL types: `numeric` = money (exact decimals), `timestamptz` = UTC date and time.

```mermaid
erDiagram
    attendances {
        uuid id PK
        uuid user_id FK
        uuid branch_id FK
        text type
        timestamptz timestamp_utc
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    audit_logs {
        uuid id PK
        uuid user_id FK "nullable"
        uuid branch_id FK
        text action
        text old_value "nullable"
        text new_value "nullable"
        timestamptz created_at_utc
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    branches {
        uuid id PK
        text name
        text alert_emails "nullable"
        timestamptz last_modified_utc
        text sync_status
        bool is_active
        uuid price_list_id FK "nullable"
        varchar code UK
    }
    cash_withdrawals {
        uuid id PK
        uuid shift_id FK
        numeric amount
        text reason
        timestamptz created_at_utc
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    categories {
        uuid id PK
        text name
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    garment_modifiers {
        uuid id PK
        uuid garment_id FK
        uuid modifier_id FK
        numeric charge_applied
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    garments {
        uuid id PK
        text code UK
        uuid ticket_id FK
        uuid product_id FK
        numeric price_charged
        text status
        text damage_evidence_json "nullable"
        text photo_url "nullable"
        text location "nullable"
        timestamptz last_modified_utc
        text sync_status
        bool is_active
        numeric list_price
    }
    guest_addresses {
        uuid id PK
        uuid guest_id FK
        text street "nullable"
        text neighborhood "nullable"
        text postal_code
        numeric latitude "nullable"
        numeric longitude "nullable"
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    guests {
        uuid id PK
        text phone UK
        text full_name
        text email "nullable"
        date birth_date "nullable"
        timestamptz last_visit_utc "nullable"
        timestamptz last_modified_utc
        text sync_status
        bool is_active
        timestamptz created_at_utc
        bool marketing_consent
        timestamptz marketing_consent_at_utc "nullable"
    }
    modifier_groups {
        uuid id PK
        uuid service_line_id FK
        text name
        int4 sort_order
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    modifiers {
        uuid id PK
        text name
        text charge_type
        numeric charge_value
        int4 sort_order
        timestamptz last_modified_utc
        text sync_status
        bool is_active
        uuid modifier_group_id FK
    }
    payments {
        uuid id PK
        uuid ticket_id FK
        uuid shift_id FK
        numeric amount
        text type
        text method
        timestamptz paid_at_utc
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    price_list_items {
        uuid id PK
        uuid price_list_id FK
        uuid product_id FK
        numeric price
        timestamptz effective_from_utc
        uuid set_by_user_id FK
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    price_lists {
        uuid id PK
        text name UK
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    product_modifiers {
        uuid id PK
        uuid product_id FK
        uuid modifier_id FK
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    products {
        uuid id PK
        uuid category_id FK
        text name
        numeric base_price
        timestamptz last_modified_utc
        text sync_status
        bool is_active
        uuid service_line_id FK
    }
    service_lines {
        uuid id PK
        text name UK
        int4 sort_order
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    shifts {
        uuid id PK
        uuid branch_id FK
        uuid user_id FK
        numeric opening_cash
        timestamptz opened_at_utc
        timestamptz closed_at_utc "nullable"
        numeric blind_cash_count "nullable"
        numeric cash_difference "nullable"
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    tickets {
        uuid id PK
        int4 folio
        text public_order_id UK
        uuid branch_id FK
        uuid guest_id FK
        uuid cashier_id FK
        uuid shift_id FK "nullable"
        timestamptz created_at_utc
        numeric subtotal
        numeric discounts
        numeric total
        numeric balance_due
        text status
        text origin
        text signature_url "nullable"
        text cancellation_reason "nullable"
        timestamptz last_modified_utc
        text sync_status
        bool is_active
    }
    users {
        uuid id PK
        text name
        text pin_hash
        text role
        uuid branch_id FK
        timestamptz last_modified_utc
        text sync_status
        bool is_active
        int4 failed_pin_attempts
        timestamptz locked_until_utc "nullable"
    }

    branches ||--o{ attendances : "branch_id"
    branches ||--o{ audit_logs : "branch_id"
    branches ||--o{ shifts : "branch_id"
    branches ||--o{ tickets : "branch_id"
    branches ||--o{ users : "branch_id"
    categories ||--o{ products : "category_id"
    garments ||--o{ garment_modifiers : "garment_id"
    guests ||--o{ guest_addresses : "guest_id"
    guests ||--o{ tickets : "guest_id"
    modifier_groups ||--o{ modifiers : "modifier_group_id"
    modifiers ||--o{ garment_modifiers : "modifier_id"
    modifiers ||--o{ product_modifiers : "modifier_id"
    price_lists |o--o{ branches : "price_list_id"
    price_lists ||--o{ price_list_items : "price_list_id"
    products ||--o{ garments : "product_id"
    products ||--o{ price_list_items : "product_id"
    products ||--o{ product_modifiers : "product_id"
    service_lines ||--o{ modifier_groups : "service_line_id"
    service_lines ||--o{ products : "service_line_id"
    shifts ||--o{ cash_withdrawals : "shift_id"
    shifts ||--o{ payments : "shift_id"
    shifts |o--o{ tickets : "shift_id"
    tickets ||--o{ garments : "ticket_id"
    tickets ||--o{ payments : "ticket_id"
    users ||--o{ attendances : "user_id"
    users |o--o{ audit_logs : "user_id"
    users ||--o{ price_list_items : "set_by_user_id"
    users ||--o{ shifts : "user_id"
    users ||--o{ tickets : "cashier_id"
```
