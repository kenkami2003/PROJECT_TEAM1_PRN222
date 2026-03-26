# Payment Module Pack

This package adds a standalone payment module (QR static + manual confirmation), with minimal impact to existing modules.

## What is included

- Customer flow:
  - List invoices to pay: `/Payment/MyInvoices`
  - Payment page per invoice: `/Payment/Invoice/{invoiceId}`
  - Customer action: "Toi da thanh toan"
- Admin flow:
  - Waiting confirmation list: `/Admin/Payment/WaitingConfirmation`
  - Confirm/Reject payment attempts

## Core business rules implemented

- One invoice can have multiple payment attempts.
- If invoice is already `Paid`, no new payment is created.
- Reuse pending non-expired payment attempt if available.
- Customer mark-paid does not mark invoice paid immediately:
  - payment -> `WaitingConfirmation`
  - set `CustomerMarkedPaidAt`
- Admin confirm:
  - payment -> `Success`
  - set `ConfirmedAt`, `PaidAt`
  - invoice -> `Paid`
- Admin reject:
  - payment -> `Rejected`
  - set `RejectedAt`, optional `RejectionNote`

## Files added

- `Controllers/KhiemndheController/PaymentController.cs`
- `Controllers/PaymentAdminController.cs`
- `Services/Payment/IPaymentService.cs`
- `Services/Payment/PaymentService.cs`
- `Services/Payment/CreateOrGetPaymentResultDto.cs`
- `Services/Payment/PaymentDto.cs`
- `Services/Payment/WaitingConfirmationPaymentDto.cs`
- `Services/Payment/TenantInvoicePaymentItemDto.cs`
- `Models/PaymentStatus.cs`
- `Models/PaymentProvider.cs`
- `Views/Khiemndhe/Payment/InvoicePayment.cshtml`
- `Views/Khiemndhe/Payment/MyInvoices.cshtml`
- `Views/Payment/WaitingConfirmation.cshtml`
- `PAYMENT_MODULE_PACK.md`

## Files updated

- `Models/Payment.cs`
- `Program.cs` (DI registration for payment service)
- `Views/Shared/_Layout.cshtml` (add customer payment button)
- `Views/Shared/_AdminLayout.cshtml` (add admin payment menu)
- `Migrations/20260325095701_PaymentFlowModule_AddColumns.cs` (payment columns only)

## Database changes (payment only)

Migration adds these nullable columns to `Payments`:

- `Status`, `Provider`
- `ProviderOrderCode`, `QrContent`, `CheckoutUrl`
- `CreatedAt`, `ExpiredAt`, `PaidAt`
- `CustomerMarkedPaidAt`, `ConfirmedAt`, `RejectedAt`
- `RejectionNote`

## Run

1. Build and run app
2. Login tenant and open:
   - `/Payment/MyInvoices`
3. Open an invoice and click payment action
4. Login admin and open:
   - `/Admin/Payment/WaitingConfirmation`

