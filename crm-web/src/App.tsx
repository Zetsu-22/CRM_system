import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Sidebar } from './components/Sidebar'
import { TopBar } from './components/TopBar'
import { ToastContainer } from './components/Toast'
import { DashboardPage } from './pages/Dashboard'
import { CompaniesPage } from './pages/Companies'
import { CounterpartiesPage } from './pages/Counterparties'
import { ItemsPage } from './pages/Items'
import { WarehousesPage } from './pages/Warehouses'
import { OrdersPage } from './pages/Orders'
import { InvoicesPage } from './pages/Invoices'
import { UpdsPage } from './pages/Upds'
import { LoginPage } from './pages/Login'
import { RegisterPage } from './pages/Register'
import { UsersPage } from './pages/Users'
import { CreateCompanyPage } from './pages/CreateCompany'
import { OrganizationsPage } from './pages/Organizations'
import { AuthProvider, useAuth } from './AuthContext'
import { OrganizationProvider } from './OrganizationContext'
import './App.css'

function ProtectedLayout() {
  const { isAuthenticated, isLoading, profile } = useAuth()
  if (isLoading) return (
    <div className="authLoading">
      <div className="authSpinner" />
      Загрузка...
    </div>
  )
  if (!isAuthenticated) return <Navigate to="/login" replace />
  return (
    <OrganizationProvider companyId={profile?.companyId ?? null}>
      <div className="appShell">
        <Sidebar />
        <div className="appContent">
          <TopBar />
          <main className="appMain">
            <Routes>
              <Route path="/"               element={<DashboardPage />} />
              <Route path="/companies"      element={<CompaniesPage />} />
              <Route path="/counterparties" element={<CounterpartiesPage />} />
              <Route path="/items"          element={<ItemsPage />} />
              <Route path="/warehouses"     element={<WarehousesPage />} />
              <Route path="/orders"         element={<OrdersPage />} />
              <Route path="/invoices"       element={<InvoicesPage />} />
              <Route path="/upds"           element={<UpdsPage />} />
              <Route path="/organizations"  element={<OrganizationsPage />} />
              <Route path="/users"          element={<UsersPage />} />
              <Route path="*"               element={<Navigate to="/" replace />} />
            </Routes>
          </main>
        </div>
        <ToastContainer />
      </div>
    </OrganizationProvider>
  )
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login"         element={<LoginPage />} />
          <Route path="/register"      element={<RegisterPage />} />
          <Route path="/setup-company" element={<SetupCompanyGuard />} />
          <Route path="/*"             element={<ProtectedLayout />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

// Guard: /setup-company only for authenticated users without a company
function SetupCompanyGuard() {
  const { isAuthenticated, isLoading, hasCompany } = useAuth()
  if (isLoading) return (
    <div className="authLoading">
      <div className="authSpinner" />
      Загрузка...
    </div>
  )
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (hasCompany)        return <Navigate to="/" replace />
  return <CreateCompanyPage />
}

export default App
