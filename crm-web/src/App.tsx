import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { Sidebar } from './components/Sidebar'
import { ToastContainer } from './components/Toast'
import { DashboardPage } from './pages/Dashboard'
import { CompaniesPage } from './pages/Companies'
import { CounterpartiesPage } from './pages/Counterparties'
import { ItemsPage } from './pages/Items'
import { WarehousesPage } from './pages/Warehouses'
import { StockPage } from './pages/Stock'
import { OrdersPage } from './pages/Orders'
import { InvoicesPage } from './pages/Invoices'
import { UpdsPage } from './pages/Upds'
import { PaymentsPage } from './pages/Payments'
import './App.css'

function App() {
  return (
    <BrowserRouter>
      <div className="appShell">
        <Sidebar />
        <main className="appMain">
          <Routes>
            <Route path="/"               element={<DashboardPage />} />
            <Route path="/companies"      element={<CompaniesPage />} />
            <Route path="/counterparties" element={<CounterpartiesPage />} />
            <Route path="/items"          element={<ItemsPage />} />
            <Route path="/warehouses"     element={<WarehousesPage />} />
            <Route path="/stock"          element={<StockPage />} />
            <Route path="/orders"         element={<OrdersPage />} />
            <Route path="/invoices"       element={<InvoicesPage />} />
            <Route path="/upds"           element={<UpdsPage />} />
            <Route path="/payments"       element={<PaymentsPage />} />
          </Routes>
        </main>
        <ToastContainer />
      </div>
    </BrowserRouter>
  )
}

export default App
