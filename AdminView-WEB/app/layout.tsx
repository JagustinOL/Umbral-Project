import type { Metadata } from 'next'
import { Geist, Geist_Mono } from 'next/font/google'
import { Analytics } from '@vercel/analytics/next'
import { AuthBootstrap } from '@/components/auth/AuthBootstrap'
import './globals.css'

const _geist = Geist({ subsets: ["latin"] });
const _geistMono = Geist_Mono({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: 'UMBRAL — Admin Dashboard',
  description: 'Mission management and operator administration for UMBRAL.',
  generator: 'v0.app',
  icons: {
    icon: [
      {
        url: '/icon-light-32x32.png',
        media: '(prefers-color-scheme: light)',
      },
      {
        url: '/icon-dark-32x32.png',
        media: '(prefers-color-scheme: dark)',
      },
      {
        url: '/icon.svg',
        type: 'image/svg+xml',
      },
    ],
    apple: '/apple-icon.png',
  },
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  const enableVercelAnalytics =
    process.env.NODE_ENV === "production" &&
    (process.env.VERCEL === "1" || process.env.NEXT_PUBLIC_ENABLE_VERCEL_ANALYTICS === "true");

  return (
    <html lang="en" className="bg-background">
      <body className="font-sans antialiased">
        <AuthBootstrap />
        {children}
        {enableVercelAnalytics && <Analytics />}
      </body>
    </html>
  )
}
