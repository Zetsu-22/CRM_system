import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider, QueryCache } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Sidebar } from './components/Sidebar'
import { TopBar } from './components/TopBar'
import { ToastContainer } from './components/Toast'
import { DashboardPage } from './pages/Dashboard'
import { CompaniesPage } from './pages/Companies'
import { CounterpartiesPage } from './pages/Counterparties'
import { ItemsPage } from './pages/Items'
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
import { toast } from './components/Toast'
import './App.css'

/**
 * Шаг 5.6 — глобальный обработчик ошибок QueryClient.
 * Любая query/mutation, не обработавшая ошибку локально, покажет toast.
 */
function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: 1,
        refetchOnWindowFocus: false,
      },
      mutations: {
        // Мутации обрабатывают ошибки локально через try/catch — глобальный handler не нужен
      },
    },
    queryCache: new QueryCache({
      onError: (error: unknown, query) => {
        // Toast только для фоновых refetch (data уже была — новый запрос провалился)
        if (query.state.data !== undefined) {
          const msg = error instanceof Error ? error.message : 'Ошибка загрузки данных'
          toast('error', msg)
        }
      },
    }),
  })
}

const queryClient = makeQueryClient()

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
    <QueryClientProvider client={queryClient}>
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
    <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
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
